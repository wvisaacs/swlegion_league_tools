using System.Text.Json;
using LeagueTools.Models;

namespace LeagueTools.Services;

public class JsonLeagueStorage : ILeagueStorage
{
    private readonly string _dataDirectory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public JsonLeagueStorage(string? dataDirectory = null)
    {
        _dataDirectory = dataDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "data");
        EnsureDirectoriesExist();
    }

    public string GetDataDirectory() => _dataDirectory;

    private void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(Path.Combine(_dataDirectory, "leagues"));
        Directory.CreateDirectory(Path.Combine(_dataDirectory, "matchings"));
        Directory.CreateDirectory(Path.Combine(_dataDirectory, "history"));
    }

    public League? LoadLeague(string eventId)
    {
        var path = GetLeaguePath(eventId);
        if (!File.Exists(path)) return null;

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<League>(json, JsonOptions);
    }

    public void SaveLeague(League league)
    {
        var path = GetLeaguePath(league.EventId);
        var json = JsonSerializer.Serialize(league, JsonOptions);
        File.WriteAllText(path, json);
    }

    public MonthlyMatchings? LoadMatchings(string eventId, string month)
    {
        var path = GetMatchingsPath(eventId, month);
        if (!File.Exists(path)) return null;

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<MonthlyMatchings>(json, JsonOptions);
    }

    public void SaveMatchings(MonthlyMatchings matchings)
    {
        var dir = Path.Combine(_dataDirectory, "matchings", matchings.EventId);
        Directory.CreateDirectory(dir);

        var path = GetMatchingsPath(matchings.EventId, matchings.Month);
        var json = JsonSerializer.Serialize(matchings, JsonOptions);
        File.WriteAllText(path, json);
    }

    private string GetLeaguePath(string eventId) =>
        Path.Combine(_dataDirectory, "leagues", $"{eventId}.json");

    private string GetMatchingsPath(string eventId, string month) =>
        Path.Combine(_dataDirectory, "matchings", eventId, $"{month.ToLowerInvariant()}.json");
}
