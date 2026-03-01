using System.Text.RegularExpressions;
using LeagueTools.Models.ListParser;

namespace LeagueTools.Services.ListParser;

/// <summary>
/// Parser for LegionHQ 2.0 text format lists.
/// </summary>
public partial class LegionHqParser : IListParser
{
    private readonly CardDatabase _cardDatabase;

    public LegionHqParser(CardDatabase cardDatabase)
    {
        _cardDatabase = cardDatabase;
    }

    public ListFormat Format => ListFormat.LegionHQ;

    /// <summary>
    /// Detects if text is in LegionHQ format.
    /// LegionHQ format has unit type headers like "Commanders:", "Corps:", etc.
    /// and units prefixed with " - ".
    /// </summary>
    public bool CanParse(string text)
    {
        // LegionHQ format indicators:
        // - Has headers like "Commanders:", "Corps:", "Operatives:", etc.
        // - Units start with " - "
        // - Commands line starts with "Commands:"
        return text.Contains("Commanders:") || 
               text.Contains("Corps:") ||
               text.Contains("Commands:") ||
               (text.Contains(" - ") && Regex.IsMatch(text, @"^\d+/\d+", RegexOptions.Multiline));
    }

    public ParsedList Parse(string text)
    {
        var result = new ParsedList
        {
            SourceFormat = ListFormat.LegionHQ,
            Author = "LeagueTools"
        };

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                       .Select(l => l.Trim())
                       .ToList();

        // Parse header: "998/1000 (12 activations)"
        ParseHeader(lines, result);

        // Parse units by section
        ParseUnits(lines, result);

        // Parse command cards
        ParseCommandCards(lines, result);

        // Detect faction from units
        DetectFaction(result);

        return result;
    }

    private void ParseHeader(List<string> lines, ParsedList result)
    {
        // Pattern: "998/1000 (12 activations)"
        var headerMatch = HeaderRegex().Match(lines.FirstOrDefault() ?? "");
        if (headerMatch.Success)
        {
            result.Points = int.Parse(headerMatch.Groups[1].Value);
            if (headerMatch.Groups[3].Success)
            {
                result.NumActivations = int.Parse(headerMatch.Groups[3].Value);
            }
        }
    }

    private void ParseUnits(List<string> lines, ParsedList result)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            // Skip section headers and non-unit lines
            if (!line.StartsWith(" - ") && !line.StartsWith("- "))
                continue;

            // Parse unit line - may return multiple units for "2× Scout Troopers"
            var units = ParseUnitLine(line);
            result.Units.AddRange(units);
        }
    }

    private List<ParsedUnit> ParseUnitLine(string line)
    {
        var result = new List<ParsedUnit>();
        
        // Remove leading " - " or "- "
        line = line.TrimStart(' ', '-').Trim();

        // Check for multiplied units: "2× Scout Troopers (48) = 96" or "2x Scout Troopers (48) = 96"
        var multiMatch = MultipliedUnitRegex().Match(line);
        if (multiMatch.Success)
        {
            var count = int.Parse(multiMatch.Groups[1].Value);
            var unitName = multiMatch.Groups[2].Value.Trim();
            
            // Create multiple units
            for (int i = 0; i < count; i++)
            {
                result.Add(new ParsedUnit
                {
                    Name = ResolveFullUnitName(unitName),
                    Upgrades = []
                });
            }
            return result;
        }

        // Pattern: "Darth Vader (170): Burst of Speed (10), Force Choke (10) = 195"
        // or: "Grand Moff Tarkin = 0" (no upgrades)
        var unitMatch = UnitWithUpgradesRegex().Match(line);
        if (unitMatch.Success)
        {
            var unitName = unitMatch.Groups[1].Value.Trim();
            var upgradesText = unitMatch.Groups[3].Value;

            var upgrades = ParseUpgrades(upgradesText);

            result.Add(new ParsedUnit
            {
                Name = ResolveFullUnitName(unitName),
                Upgrades = upgrades
            });
            return result;
        }

        // Simple unit without upgrades: "Grand Moff Tarkin = 0"
        var simpleMatch = SimpleUnitRegex().Match(line);
        if (simpleMatch.Success)
        {
            result.Add(new ParsedUnit
            {
                Name = ResolveFullUnitName(simpleMatch.Groups[1].Value.Trim()),
                Upgrades = []
            });
            return result;
        }

        return result;
    }

    private List<string> ParseUpgrades(string upgradesText)
    {
        if (string.IsNullOrWhiteSpace(upgradesText))
            return [];

        var upgrades = new List<string>();
        
        // Split by comma, then extract upgrade names
        // Pattern: "Upgrade Name (cost)" or just "Upgrade Name"
        var parts = upgradesText.Split(',');
        foreach (var part in parts)
        {
            var match = UpgradeRegex().Match(part.Trim());
            if (match.Success)
            {
                var upgradeName = match.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(upgradeName))
                {
                    upgrades.Add(ResolveFullUpgradeName(upgradeName));
                }
            }
        }

        return upgrades;
    }

    private void ParseCommandCards(List<string> lines, ParsedList result)
    {
        foreach (var line in lines)
        {
            if (!line.StartsWith("Commands:"))
                continue;

            // Pattern: "Commands: • Implacable, • Vader's Might, •• Push, ••• Coordinated Fire, •••• Standing Orders"
            var commandsText = line["Commands:".Length..].Trim();
            
            // Split by comma and extract command names (removing pip symbols)
            var commands = commandsText.Split(',')
                .Select(c => 
                {
                    var trimmed = c.Trim();
                    // Remove all non-letter/digit characters from the start (handles all bullet variations)
                    var startIdx = 0;
                    while (startIdx < trimmed.Length)
                    {
                        var ch = trimmed[startIdx];
                        // Allow letters, digits, apostrophes, and hyphens
                        if (char.IsLetter(ch) || char.IsDigit(ch))
                            break;
                        startIdx++;
                    }
                    return startIdx < trimmed.Length ? trimmed[startIdx..].Trim() : string.Empty;
                })
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList();

            result.CommandCards = commands;
            break;
        }
    }

    private void DetectFaction(ParsedList result)
    {
        // Try to detect faction from the first unique unit
        foreach (var unit in result.Units)
        {
            var faction = _cardDatabase.DetectFactionFromUnit(unit.Name);
            if (!string.IsNullOrEmpty(faction))
            {
                result.ArmyFaction = NormalizeFaction(faction);
                return;
            }
        }

        // Fallback: try to detect from any unit
        foreach (var unit in result.Units)
        {
            var card = _cardDatabase.GetByName(unit.Name);
            if (card?.Faction != null)
            {
                result.ArmyFaction = NormalizeFaction(card.Faction);
                return;
            }
        }
    }

    private string ResolveFullUnitName(string shortName)
    {
        var card = _cardDatabase.GetByName(shortName);
        return card?.FullName ?? shortName;
    }

    private string ResolveFullUpgradeName(string shortName)
    {
        var card = _cardDatabase.GetByName(shortName);
        return card?.CardName ?? shortName;
    }

    private static string NormalizeFaction(string faction)
    {
        // Normalize to the format used in JSON exports
        return faction.ToLowerInvariant() switch
        {
            "rebels" => "rebel",
            "empire" => "empire",
            "republic" => "republic",
            "separatists" => "separatist",
            "separatist" => "separatist",
            _ => faction.ToLowerInvariant()
        };
    }

    // Regex patterns
    [GeneratedRegex(@"^(\d+)/(\d+)(?:\s*\((\d+)\s*activations?\))?", RegexOptions.IgnoreCase)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^(\d+)[×x]\s*(.+?)\s*\((\d+)\)(?:\s*=\s*\d+)?$", RegexOptions.IgnoreCase)]
    private static partial Regex MultipliedUnitRegex();

    [GeneratedRegex(@"^(.+?)\s*\((\d+)\):\s*(.+?)\s*=\s*\d+$")]
    private static partial Regex UnitWithUpgradesRegex();

    [GeneratedRegex(@"^(.+?)\s*(?:\(\d+\))?\s*=\s*\d+$")]
    private static partial Regex SimpleUnitRegex();

    [GeneratedRegex(@"^([^(]+)(?:\s*\(\d+\))?$")]
    private static partial Regex UpgradeRegex();

    [GeneratedRegex(@"^[•]+\s*")]
    private static partial Regex CommandPipRegex();
}
