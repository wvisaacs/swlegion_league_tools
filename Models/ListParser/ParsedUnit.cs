using System.Text.Json.Serialization;

namespace LeagueTools.Models.ListParser;

/// <summary>
/// Represents a unit in a parsed Star Wars Legion list.
/// </summary>
public class ParsedUnit
{
    /// <summary>
    /// The full name of the unit including subtitle (e.g., "Darth Vader Dark Lord of the Sith").
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// List of upgrade card names equipped to this unit.
    /// </summary>
    [JsonPropertyName("upgrades")]
    public List<string> Upgrades { get; set; } = [];

    /// <summary>
    /// List of loadout upgrade card names (set-aside upgrades for units with Loadout keyword).
    /// </summary>
    [JsonPropertyName("loadout")]
    public List<string> Loadout { get; set; } = [];
}
