using System.Text.Json.Serialization;

namespace LeagueTools.Models;

public class LeagueConfiguration
{
    [JsonPropertyName("podSize")]
    public int PodSize { get; set; } = 3;

    [JsonPropertyName("months")]
    public List<string> Months { get; set; } = new() { "February", "March", "April" };

    public void Validate()
    {
        if (PodSize < 2)
            throw new ArgumentException("Pod size must be at least 2");
        if (Months.Count == 0)
            throw new ArgumentException("At least one month must be specified");
    }
}
