using LeagueTools.Models.ListParser;

namespace LeagueTools.Services.ListParser;

/// <summary>
/// Interface for parsing Star Wars Legion army lists from text format.
/// </summary>
public interface IListParser
{
    /// <summary>
    /// The format this parser handles.
    /// </summary>
    ListFormat Format { get; }

    /// <summary>
    /// Attempts to detect if the given text is in this parser's format.
    /// </summary>
    /// <param name="text">The text to analyze.</param>
    /// <returns>True if this parser can likely handle the text.</returns>
    bool CanParse(string text);

    /// <summary>
    /// Parses the text into a structured list.
    /// </summary>
    /// <param name="text">The text representation of the army list.</param>
    /// <returns>A parsed list object.</returns>
    ParsedList Parse(string text);
}
