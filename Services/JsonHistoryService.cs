using System.Text.Json;
using LeagueTools.Models;

namespace LeagueTools.Services;

public class JsonHistoryService : IHistoryService
{
    private readonly string _dataDirectory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public JsonHistoryService(string? dataDirectory = null)
    {
        _dataDirectory = dataDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "data");
        Directory.CreateDirectory(Path.Combine(_dataDirectory, "history"));
    }

    public MatchHistory LoadHistory(string eventId)
    {
        var path = GetHistoryPath(eventId);
        if (!File.Exists(path))
        {
            return new MatchHistory { EventId = eventId };
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<MatchHistory>(json, JsonOptions) 
            ?? new MatchHistory { EventId = eventId };
    }

    public void SaveHistory(string eventId, MatchHistory history)
    {
        var path = GetHistoryPath(eventId);
        var json = JsonSerializer.Serialize(history, JsonOptions);
        File.WriteAllText(path, json);
    }

    public void RecordMatches(string eventId, MonthlyMatchings matchings)
    {
        var history = LoadHistory(eventId);

        foreach (var pod in matchings.Pods)
        {
            foreach (var match in pod.Matches)
            {
                // Avoid duplicates
                if (!history.HavePlayed(match.Player1Id, match.Player2Id) ||
                    !history.Pairings.Any(p => p.IsPairing(match.Player1Id, match.Player2Id) && p.Month == matchings.Month))
                {
                    history.AddPairing(match.Player1Id, match.Player2Id, matchings.Month);
                }
            }
        }

        SaveHistory(eventId, history);
    }

    private string GetHistoryPath(string eventId) =>
        Path.Combine(_dataDirectory, "history", $"{eventId}.json");
}
