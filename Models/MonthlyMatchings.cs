using System.Text.Json.Serialization;

namespace LeagueTools.Models;

public class MonthlyMatchings
{
    [JsonPropertyName("eventId")]
    public string EventId { get; set; } = string.Empty;

    [JsonPropertyName("month")]
    public string Month { get; set; } = string.Empty;

    [JsonPropertyName("podSize")]
    public int TargetPodSize { get; set; }

    [JsonPropertyName("generatedAt")]
    public DateTime GeneratedAt { get; set; }

    [JsonPropertyName("pods")]
    public List<Pod> Pods { get; set; } = new();

    public int TotalMatches => Pods.Sum(p => p.Matches.Count);
    public int TotalPlayers => Pods.Sum(p => p.PlayerIds.Count);
}
