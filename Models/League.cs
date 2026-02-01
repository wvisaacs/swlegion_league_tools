using System.Text.Json.Serialization;

namespace LeagueTools.Models;

public class League
{
    [JsonPropertyName("eventId")]
    public string EventId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("players")]
    public List<Player> Players { get; set; } = new();

    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; }
}
