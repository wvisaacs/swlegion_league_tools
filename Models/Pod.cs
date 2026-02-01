using System.Text.Json.Serialization;

namespace LeagueTools.Models;

public class Pod
{
    [JsonPropertyName("podId")]
    public int PodId { get; set; }

    [JsonPropertyName("isOverflow")]
    public bool IsOverflow { get; set; }

    [JsonPropertyName("playerIds")]
    public List<string> PlayerIds { get; set; } = new();

    [JsonPropertyName("matches")]
    public List<Match> Matches { get; set; } = new();

    public int Size => PlayerIds.Count;
}
