using System.Text.Json.Serialization;

namespace LeagueTools.Models.ListParser;

/// <summary>
/// Represents a card from the LegionHQ2 card database.
/// </summary>
public class LegionCard
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("cardName")]
    public string CardName { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("cardType")]
    public string CardType { get; set; } = string.Empty;

    [JsonPropertyName("cardSubtype")]
    public string? CardSubtype { get; set; }

    [JsonPropertyName("faction")]
    public string? Faction { get; set; }

    [JsonPropertyName("rank")]
    public string? Rank { get; set; }

    [JsonPropertyName("cost")]
    public int Cost { get; set; }

    [JsonPropertyName("isUnique")]
    public bool IsUnique { get; set; }

    [JsonPropertyName("keywords")]
    public List<string>? Keywords { get; set; }

    [JsonPropertyName("upgradeBar")]
    public List<string>? UpgradeBar { get; set; }

    [JsonPropertyName("requirements")]
    public List<string>? Requirements { get; set; }

    /// <summary>
    /// The subtitle for unique units (e.g., "Dark Lord of the Sith" for Darth Vader).
    /// This may be populated from an external source or derived.
    /// </summary>
    [JsonIgnore]
    public string? Subtitle { get; set; }

    /// <summary>
    /// Gets the full name including subtitle if available.
    /// </summary>
    public string FullName => string.IsNullOrEmpty(Subtitle) 
        ? CardName 
        : $"{CardName} {Subtitle}";
}
