using LeagueTools.Models.ListParser;

namespace LeagueTools.Services.ListParser;

/// <summary>
/// Interface for exporting parsed lists to various formats.
/// </summary>
public interface IListExporter
{
    /// <summary>
    /// Exports the parsed list to JSON format.
    /// </summary>
    string ExportToJson(ParsedList list, bool indented = true);

    /// <summary>
    /// Generates a shareable URL for the list, if supported.
    /// </summary>
    /// <returns>The URL, or null if URL generation is not supported.</returns>
    string? GenerateUrl(ParsedList list);
}
