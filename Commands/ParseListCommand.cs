using System.CommandLine;
using LeagueTools.Services.ListParser;

namespace LeagueTools.Commands;

public class ParseListCommand : Command
{
    public ParseListCommand() : base("parse-list", "Parse a Star Wars Legion army list from text format to JSON")
    {
        var inputArg = new Argument<string?>("input", () => null, 
            "Input file path. If not provided, reads from stdin.");

        var formatOption = new Option<string?>(
            ["--format", "-f"],
            "Source format: 'legionhq' or 'tabletopadmiral'. Auto-detected if not specified.");

        var outputOption = new Option<string?>(
            ["--output", "-o"],
            "Output file path. If not provided, outputs to stdout.");

        var exportFormatOption = new Option<string>(
            ["--export", "-e"],
            () => "legionhq",
            "Export format: 'legionhq' or 'tabletopadmiral'. Default: legionhq");

        var urlOption = new Option<bool>(
            ["--url", "-u"],
            "Also output a shareable URL (if supported by the export format).");

        var compactOption = new Option<bool>(
            ["--compact", "-c"],
            "Output compact JSON (no indentation).");

        AddArgument(inputArg);
        AddOption(formatOption);
        AddOption(outputOption);
        AddOption(exportFormatOption);
        AddOption(urlOption);
        AddOption(compactOption);

        this.SetHandler(async (input, format, output, exportFormat, showUrl, compact) =>
        {
            try
            {
                await ExecuteAsync(input, format, output, exportFormat, showUrl, compact);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, inputArg, formatOption, outputOption, exportFormatOption, urlOption, compactOption);
    }

    private static async Task ExecuteAsync(
        string? input, 
        string? format, 
        string? output, 
        string exportFormat,
        bool showUrl,
        bool compact)
    {
        // Read input text
        string inputText;
        if (string.IsNullOrEmpty(input))
        {
            // Read from stdin
            inputText = await Console.In.ReadToEndAsync();
        }
        else
        {
            if (!File.Exists(input))
            {
                throw new FileNotFoundException($"Input file not found: {input}");
            }
            inputText = await File.ReadAllTextAsync(input);
        }

        if (string.IsNullOrWhiteSpace(inputText))
        {
            throw new InvalidOperationException("Input is empty.");
        }

        // Load card database
        var cardDatabase = new CardDatabase();
        var dataPath = Path.Combine(AppContext.BaseDirectory, "data", "legionhq-cards.json");
        
        // Try alternate paths
        if (!File.Exists(dataPath))
        {
            dataPath = Path.Combine(Directory.GetCurrentDirectory(), "data", "legionhq-cards.json");
        }
        if (!File.Exists(dataPath))
        {
            // Look relative to the source directory
            var srcDir = Directory.GetCurrentDirectory();
            while (srcDir != null && !File.Exists(Path.Combine(srcDir, "data", "legionhq-cards.json")))
            {
                srcDir = Directory.GetParent(srcDir)?.FullName;
            }
            if (srcDir != null)
            {
                dataPath = Path.Combine(srcDir, "data", "legionhq-cards.json");
            }
        }

        if (!File.Exists(dataPath))
        {
            throw new FileNotFoundException(
                "Card database not found. Expected at: data/legionhq-cards.json");
        }

        await cardDatabase.LoadAsync(dataPath);

        // Create parsers
        var legionHqParser = new LegionHqParser(cardDatabase);
        var ttaParser = new TabletopAdmiralParser(cardDatabase);
        
        // Select parser
        IListParser parser;
        if (!string.IsNullOrEmpty(format))
        {
            parser = format.ToLowerInvariant() switch
            {
                "legionhq" or "lhq" => legionHqParser,
                "tabletopadmiral" or "tta" => ttaParser,
                _ => throw new ArgumentException($"Unknown format: {format}")
            };
        }
        else
        {
            // Auto-detect
            if (legionHqParser.CanParse(inputText))
            {
                parser = legionHqParser;
            }
            else if (ttaParser.CanParse(inputText))
            {
                parser = ttaParser;
            }
            else
            {
                throw new InvalidOperationException(
                    "Could not detect input format. Use --format to specify.");
            }
        }

        // Parse the list
        var parsedList = parser.Parse(inputText);

        // Select exporter
        IListExporter exporter = exportFormat.ToLowerInvariant() switch
        {
            "legionhq" or "lhq" => new LegionHqExporter(cardDatabase),
            "tabletopadmiral" or "tta" => new TabletopAdmiralExporter(),
            _ => throw new ArgumentException($"Unknown export format: {exportFormat}")
        };

        // Export to JSON
        var json = exporter.ExportToJson(parsedList, indented: !compact);

        // Output
        if (string.IsNullOrEmpty(output))
        {
            Console.WriteLine(json);
        }
        else
        {
            await File.WriteAllTextAsync(output, json);
            Console.WriteLine($"Output written to: {output}");
        }

        // Show URL if requested
        if (showUrl)
        {
            var url = exporter.GenerateUrl(parsedList);
            if (url != null)
            {
                Console.WriteLine();
                Console.WriteLine($"URL: {url}");
            }
            else
            {
                Console.Error.WriteLine("Note: URL generation not available for this export format.");
            }
        }
    }
}
