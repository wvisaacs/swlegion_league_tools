using System.Text.Json.Serialization;

namespace LeagueTools.Models.ListParser;

/// <summary>
/// Represents the battlefield deck selections (objectives, deployments, conditions).
/// </summary>
public class BattlefieldDeck
{
    [JsonPropertyName("scenario")]
    public string Scenario { get; set; } = "standard";

    [JsonPropertyName("objective")]
    public List<string> Objective { get; set; } = [];

    [JsonPropertyName("deployment")]
    public List<string> Deployment { get; set; } = [];

    [JsonPropertyName("conditions")]
    public List<string> Conditions { get; set; } = [];
}
