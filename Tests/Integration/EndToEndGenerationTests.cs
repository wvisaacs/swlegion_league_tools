using LeagueTools.Services;
using LeagueTools.Tests.Fixtures;

namespace LeagueTools.Tests.Integration;

public class EndToEndGenerationTests : IDisposable
{
    private readonly string _testDataDir;
    private readonly JsonLeagueStorage _storage;
    private readonly JsonHistoryService _historyService;
    private readonly MatchingGenerator _generator;

    public EndToEndGenerationTests()
    {
        _testDataDir = Path.Combine(Path.GetTempPath(), $"league_tools_e2e_{Guid.NewGuid()}");
        _storage = new JsonLeagueStorage(_testDataDir);
        _historyService = new JsonHistoryService(_testDataDir);
        _generator = new MatchingGenerator();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDataDir))
        {
            Directory.Delete(_testDataDir, true);
        }
    }

    [Fact]
    public void FullWorkflow_GenerateAndSaveMatchings()
    {
        // Simulate the full workflow
        var league = TestLeagueFactory.CreateLeague(14, "e2e-test");
        _storage.SaveLeague(league);

        // Load league back
        var loadedLeague = _storage.LoadLeague("e2e-test");
        Assert.NotNull(loadedLeague);

        // Generate matchings
        var history = _historyService.LoadHistory("e2e-test");
        var matchings = _generator.Generate(loadedLeague, 3, history, "February", seed: 42);

        // Save matchings and history
        _storage.SaveMatchings(matchings);
        _historyService.RecordMatches("e2e-test", matchings);

        // Verify
        var loadedMatchings = _storage.LoadMatchings("e2e-test", "February");
        Assert.NotNull(loadedMatchings);
        Assert.Equal(14, loadedMatchings.TotalPlayers);
        MatchingAssertions.AssertValidMatchings(loadedMatchings, loadedLeague);
    }

    [Fact]
    public void FullWorkflow_MultipleMonthsWithHistory()
    {
        var league = TestLeagueFactory.CreateLeague(9, "multi-month");
        _storage.SaveLeague(league);

        // February
        var febHistory = _historyService.LoadHistory("multi-month");
        var febMatchings = _generator.Generate(league, 3, febHistory, "February", seed: 100);
        _storage.SaveMatchings(febMatchings);
        _historyService.RecordMatches("multi-month", febMatchings);

        // March (should use February history)
        var marchHistory = _historyService.LoadHistory("multi-month");
        Assert.NotEmpty(marchHistory.Pairings); // Should have February pairings
        
        var marchMatchings = _generator.Generate(league, 3, marchHistory, "March", seed: 200);
        _storage.SaveMatchings(marchMatchings);
        _historyService.RecordMatches("multi-month", marchMatchings);

        // Verify both months
        var loadedFeb = _storage.LoadMatchings("multi-month", "February");
        var loadedMarch = _storage.LoadMatchings("multi-month", "March");
        
        Assert.NotNull(loadedFeb);
        Assert.NotNull(loadedMarch);
        Assert.Equal(9, loadedFeb.TotalPlayers);
        Assert.Equal(9, loadedMarch.TotalPlayers);

        // History should have pairings from both months
        var finalHistory = _historyService.LoadHistory("multi-month");
        Assert.True(finalHistory.Pairings.Any(p => p.Month == "February"));
        Assert.True(finalHistory.Pairings.Any(p => p.Month == "March"));
    }

    [Fact]
    public void FullWorkflow_WithFixedPods()
    {
        var league = TestLeagueFactory.CreateLeague(9, "fixed-pod-test");
        _storage.SaveLeague(league);

        var history = _historyService.LoadHistory("fixed-pod-test");
        var fixedPods = new List<List<string>> { new() { "1", "2", "3" } };

        var matchings = _generator.Generate(league, 3, history, "February", fixedPods, seed: 42);
        _storage.SaveMatchings(matchings);

        // Verify fixed pod is first
        Assert.Equal(3, matchings.Pods.Count);
        var firstPod = matchings.Pods[0];
        Assert.Contains("1", firstPod.PlayerIds);
        Assert.Contains("2", firstPod.PlayerIds);
        Assert.Contains("3", firstPod.PlayerIds);

        MatchingAssertions.AssertValidMatchings(matchings, league);
    }

    [Fact]
    public void LargeLeague_HandlesCorrectly()
    {
        // Test with a larger league
        var league = TestLeagueFactory.CreateLeague(50, "large-league");
        var history = TestHistoryFactory.CreateEmpty("large-league");

        var matchings = _generator.Generate(league, 4, history, "February", seed: 42);

        Assert.Equal(50, matchings.TotalPlayers);
        MatchingAssertions.AssertValidMatchings(matchings, league);
    }

    [Fact]
    public void DifferentPodSizes_AllWork()
    {
        var league = TestLeagueFactory.CreateLeague(12, "pod-sizes");
        var history = TestHistoryFactory.CreateEmpty("pod-sizes");

        // Test various pod sizes
        foreach (var podSize in new[] { 2, 3, 4, 5, 6 })
        {
            var matchings = _generator.Generate(league, podSize, history, $"Test{podSize}", seed: 42);
            Assert.Equal(12, matchings.TotalPlayers);
            MatchingAssertions.AssertValidMatchings(matchings, league);
        }
    }
}
