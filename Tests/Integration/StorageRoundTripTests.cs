using LeagueTools.Models;
using LeagueTools.Services;
using LeagueTools.Tests.Fixtures;

namespace LeagueTools.Tests.Integration;

public class StorageRoundTripTests : IDisposable
{
    private readonly string _testDataDir;
    private readonly JsonLeagueStorage _storage;
    private readonly JsonHistoryService _historyService;

    public StorageRoundTripTests()
    {
        _testDataDir = Path.Combine(Path.GetTempPath(), $"league_tools_test_{Guid.NewGuid()}");
        _storage = new JsonLeagueStorage(_testDataDir);
        _historyService = new JsonHistoryService(_testDataDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDataDir))
        {
            Directory.Delete(_testDataDir, true);
        }
    }

    [Fact]
    public void League_SaveAndLoad_PreservesData()
    {
        var league = TestLeagueFactory.CreateLeague(5, "test-123");
        league.Name = "Test League Name";
        league.Url = "https://test.com/event/123";

        _storage.SaveLeague(league);
        var loaded = _storage.LoadLeague("test-123");

        Assert.NotNull(loaded);
        Assert.Equal(league.EventId, loaded.EventId);
        Assert.Equal(league.Name, loaded.Name);
        Assert.Equal(league.Url, loaded.Url);
        Assert.Equal(league.Players.Count, loaded.Players.Count);
        
        for (int i = 0; i < league.Players.Count; i++)
        {
            Assert.Equal(league.Players[i].Id, loaded.Players[i].Id);
            Assert.Equal(league.Players[i].Name, loaded.Players[i].Name);
        }
    }

    [Fact]
    public void League_LoadNonExistent_ReturnsNull()
    {
        var loaded = _storage.LoadLeague("non-existent");

        Assert.Null(loaded);
    }

    [Fact]
    public void Matchings_SaveAndLoad_PreservesData()
    {
        var league = TestLeagueFactory.CreateLeague(6, "test-456");
        var generator = new MatchingGenerator();
        var history = TestHistoryFactory.CreateEmpty("test-456");

        var matchings = generator.Generate(league, 3, history, "February", seed: 42);
        _storage.SaveMatchings(matchings);
        var loaded = _storage.LoadMatchings("test-456", "February");

        Assert.NotNull(loaded);
        Assert.Equal(matchings.EventId, loaded.EventId);
        Assert.Equal(matchings.Month, loaded.Month);
        Assert.Equal(matchings.Pods.Count, loaded.Pods.Count);
        Assert.Equal(matchings.TotalMatches, loaded.TotalMatches);
    }

    [Fact]
    public void Matchings_LoadNonExistent_ReturnsNull()
    {
        var loaded = _storage.LoadMatchings("non-existent", "February");

        Assert.Null(loaded);
    }

    [Fact]
    public void History_SaveAndLoad_PreservesData()
    {
        var history = TestHistoryFactory.CreateWithPairings("test-789",
            ("1", "2", "February"),
            ("1", "3", "February"),
            ("2", "3", "March"));

        _historyService.SaveHistory("test-789", history);
        var loaded = _historyService.LoadHistory("test-789");

        Assert.Equal(history.EventId, loaded.EventId);
        Assert.Equal(history.Pairings.Count, loaded.Pairings.Count);
        Assert.True(loaded.HavePlayed("1", "2"));
        Assert.True(loaded.HavePlayed("1", "3"));
        Assert.True(loaded.HavePlayed("2", "3"));
    }

    [Fact]
    public void History_LoadNonExistent_ReturnsEmpty()
    {
        var loaded = _historyService.LoadHistory("non-existent");

        Assert.NotNull(loaded);
        Assert.Equal("non-existent", loaded.EventId);
        Assert.Empty(loaded.Pairings);
    }

    [Fact]
    public void RecordMatches_AddsToHistory()
    {
        var league = TestLeagueFactory.CreateLeague(6, "test-record");
        var generator = new MatchingGenerator();
        var history = _historyService.LoadHistory("test-record");

        var matchings = generator.Generate(league, 3, history, "February", seed: 42);
        _historyService.RecordMatches("test-record", matchings);

        var loaded = _historyService.LoadHistory("test-record");
        Assert.NotEmpty(loaded.Pairings);
        Assert.All(loaded.Pairings, p => Assert.Equal("February", p.Month));
    }
}
