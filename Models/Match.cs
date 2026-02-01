using System.Text.Json.Serialization;

namespace LeagueTools.Models;

public class Match
{
    [JsonPropertyName("player1Id")]
    public string Player1Id { get; set; } = string.Empty;

    [JsonPropertyName("player2Id")]
    public string Player2Id { get; set; } = string.Empty;

    [JsonPropertyName("player1Name")]
    public string Player1Name { get; set; } = string.Empty;

    [JsonPropertyName("player2Name")]
    public string Player2Name { get; set; } = string.Empty;

    public bool Involves(string playerId) => Player1Id == playerId || Player2Id == playerId;

    public string GetOpponentId(string playerId) =>
        Player1Id == playerId ? Player2Id : Player1Id;

    public override string ToString() => $"{Player1Name} vs {Player2Name}";
}
