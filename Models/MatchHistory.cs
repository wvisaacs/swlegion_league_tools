using System.Text.Json.Serialization;

namespace LeagueTools.Models;

public class HistoricalPairing
{
    [JsonPropertyName("player1Id")]
    public string Player1Id { get; set; } = string.Empty;

    [JsonPropertyName("player2Id")]
    public string Player2Id { get; set; } = string.Empty;

    [JsonPropertyName("month")]
    public string Month { get; set; } = string.Empty;

    public bool Involves(string playerId) => Player1Id == playerId || Player2Id == playerId;

    public bool IsPairing(string id1, string id2) =>
        (Player1Id == id1 && Player2Id == id2) || (Player1Id == id2 && Player2Id == id1);
}

public class MatchHistory
{
    [JsonPropertyName("eventId")]
    public string EventId { get; set; } = string.Empty;

    [JsonPropertyName("pairings")]
    public List<HistoricalPairing> Pairings { get; set; } = new();

    public bool HavePlayed(string player1Id, string player2Id) =>
        Pairings.Any(p => p.IsPairing(player1Id, player2Id));

    public int TimesPlayed(string player1Id, string player2Id) =>
        Pairings.Count(p => p.IsPairing(player1Id, player2Id));

    public List<string> GetPreviousOpponents(string playerId) =>
        Pairings
            .Where(p => p.Involves(playerId))
            .Select(p => p.Player1Id == playerId ? p.Player2Id : p.Player1Id)
            .Distinct()
            .ToList();

    public void AddPairing(string player1Id, string player2Id, string month)
    {
        Pairings.Add(new HistoricalPairing
        {
            Player1Id = player1Id,
            Player2Id = player2Id,
            Month = month
        });
    }
}
