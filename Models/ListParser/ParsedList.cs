using System.Text.Json.Serialization;

namespace LeagueTools.Models.ListParser;

/// <summary>
/// Represents a fully parsed Star Wars Legion army list.
/// Compatible with both LegionHQ2 and Tabletop Admiral JSON formats.
/// </summary>
public class ParsedList
{
    /// <summary>
    /// Name of the list (optional).
    /// </summary>
    [JsonPropertyName("listname")]
    public string? ListName { get; set; }

    /// <summary>
    /// The tool/author that generated this list.
    /// </summary>
    [JsonPropertyName("author")]
    public string Author { get; set; } = "LeagueTools";

    /// <summary>
    /// Total points cost of the list.
    /// </summary>
    [JsonPropertyName("points")]
    public int Points { get; set; }

    /// <summary>
    /// Number of activations in the list.
    /// </summary>
    [JsonPropertyName("numActivations")]
    public int NumActivations { get; set; }

    /// <summary>
    /// The army faction: "empire", "rebel", "republic", or "separatist".
    /// </summary>
    [JsonPropertyName("armyFaction")]
    public string ArmyFaction { get; set; } = string.Empty;

    /// <summary>
    /// Battle force name, if applicable (e.g., "Blizzard Force", "Echo Base Defenders").
    /// </summary>
    [JsonPropertyName("battleForce")]
    public string? BattleForce { get; set; }

    /// <summary>
    /// List of command card names.
    /// </summary>
    [JsonPropertyName("commandCards")]
    public List<string> CommandCards { get; set; } = [];

    /// <summary>
    /// List of contingency command card names.
    /// </summary>
    [JsonPropertyName("contingencies")]
    public List<string> Contingencies { get; set; } = [];

    /// <summary>
    /// List of units in the army.
    /// </summary>
    [JsonPropertyName("units")]
    public List<ParsedUnit> Units { get; set; } = [];

    /// <summary>
    /// The battlefield deck selections.
    /// </summary>
    [JsonPropertyName("battlefieldDeck")]
    public BattlefieldDeck BattlefieldDeck { get; set; } = new();

    /// <summary>
    /// Generated list link (URL), if available.
    /// </summary>
    [JsonPropertyName("listlink")]
    public string? ListLink { get; set; }

    /// <summary>
    /// The source format this list was parsed from.
    /// </summary>
    [JsonIgnore]
    public ListFormat SourceFormat { get; set; }
}
