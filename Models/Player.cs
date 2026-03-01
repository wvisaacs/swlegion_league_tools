using System.Text.Json.Serialization;

namespace LeagueTools.Models;

public class Player
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("faction")]
    public string? Faction { get; set; }

    [JsonPropertyName("rating")]
    public int? Rating { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("wins")]
    public int? Wins { get; set; }

    [JsonPropertyName("losses")]
    public int? Losses { get; set; }

    public string DisplayName => $"{Name} #{Id}";

    public override string ToString() => DisplayName;
}
