using LeagueTools.Services;
using LeagueTools.Tests.Fixtures;

namespace LeagueTools.Tests.Unit;

public class FixedPodsTests
{
    private readonly MatchingGenerator _generator = new();

    [Fact]
    public void Generate_WithFixedPod_CreatesFixedPodFirst()
    {
        var league = TestLeagueFactory.CreateLeague(9);
        var history = TestHistoryFactory.CreateEmpty();
        var fixedPods = new List<List<string>> { new() { "1", "2", "3" } };

        var result = _generator.Generate(league, 3, history, "February", fixedPods);

        Assert.Equal(3, result.Pods.Count);
        
        // First pod should be the fixed one
        var firstPod = result.Pods[0];
        Assert.Contains("1", firstPod.PlayerIds);
        Assert.Contains("2", firstPod.PlayerIds);
        Assert.Contains("3", firstPod.PlayerIds);
        
        MatchingAssertions.AssertValidMatchings(result, league);
    }

    [Fact]
    public void Generate_WithMultipleFixedPods_CreatesAllFixedPodsFirst()
    {
        var league = TestLeagueFactory.CreateLeague(9);
        var history = TestHistoryFactory.CreateEmpty();
        var fixedPods = new List<List<string>> 
        { 
            new() { "1", "2", "3" },
            new() { "4", "5" }
        };

        var result = _generator.Generate(league, 3, history, "February", fixedPods);

        // First two pods should be fixed
        var pod1 = result.Pods[0];
        var pod2 = result.Pods[1];
        
        Assert.Equal(3, pod1.Size);
        Assert.Equal(2, pod2.Size);
        Assert.Contains("1", pod1.PlayerIds);
        Assert.Contains("4", pod2.PlayerIds);
        
        MatchingAssertions.AssertValidMatchings(result, league);
    }

    [Fact]
    public void Generate_FixedPodLargerThanTarget_MarkedAsOverflow()
    {
        var league = TestLeagueFactory.CreateLeague(10);
        var history = TestHistoryFactory.CreateEmpty();
        var fixedPods = new List<List<string>> { new() { "1", "2", "3", "4" } };

        var result = _generator.Generate(league, 3, history, "February", fixedPods);

        var fixedPod = result.Pods[0];
        Assert.Equal(4, fixedPod.Size);
        Assert.True(fixedPod.IsOverflow);
        
        MatchingAssertions.AssertValidMatchings(result, league);
    }

    [Fact]
    public void Generate_FixedPodSmallerThanTarget_NotMarkedAsOverflow()
    {
        var league = TestLeagueFactory.CreateLeague(10);
        var history = TestHistoryFactory.CreateEmpty();
        var fixedPods = new List<List<string>> { new() { "1", "2" } };

        var result = _generator.Generate(league, 3, history, "February", fixedPods);

        var fixedPod = result.Pods[0];
        Assert.Equal(2, fixedPod.Size);
        Assert.False(fixedPod.IsOverflow);
        
        MatchingAssertions.AssertValidMatchings(result, league);
    }

    [Fact]
    public void Generate_AllPlayersInFixedPods_NoRandomDistribution()
    {
        var league = TestLeagueFactory.CreateLeague(6);
        var history = TestHistoryFactory.CreateEmpty();
        var fixedPods = new List<List<string>> 
        { 
            new() { "1", "2", "3" },
            new() { "4", "5", "6" }
        };

        var result = _generator.Generate(league, 3, history, "February", fixedPods);

        Assert.Equal(2, result.Pods.Count);
        MatchingAssertions.AssertValidMatchings(result, league);
    }

    [Fact]
    public void Generate_FixedPodWithInvalidPlayerId_ThrowsException()
    {
        var league = TestLeagueFactory.CreateLeague(5);
        var history = TestHistoryFactory.CreateEmpty();
        var fixedPods = new List<List<string>> { new() { "1", "999" } };

        var ex = Assert.Throws<ArgumentException>(() => 
            _generator.Generate(league, 3, history, "February", fixedPods));
        
        Assert.Contains("999", ex.Message);
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void Generate_PlayerInMultipleFixedPods_ThrowsException()
    {
        var league = TestLeagueFactory.CreateLeague(6);
        var history = TestHistoryFactory.CreateEmpty();
        var fixedPods = new List<List<string>> 
        { 
            new() { "1", "2", "3" },
            new() { "3", "4", "5" }  // Player 3 is duplicated
        };

        var ex = Assert.Throws<ArgumentException>(() => 
            _generator.Generate(league, 3, history, "February", fixedPods));
        
        Assert.Contains("3", ex.Message);
        Assert.Contains("multiple", ex.Message.ToLower());
    }

    [Fact]
    public void Generate_FixedPodWithOnePlayer_ThrowsException()
    {
        var league = TestLeagueFactory.CreateLeague(5);
        var history = TestHistoryFactory.CreateEmpty();
        var fixedPods = new List<List<string>> { new() { "1" } };

        var ex = Assert.Throws<ArgumentException>(() => 
            _generator.Generate(league, 3, history, "February", fixedPods));
        
        Assert.Contains("at least 2", ex.Message);
    }

    [Fact]
    public void Generate_EmptyFixedPods_WorksNormally()
    {
        var league = TestLeagueFactory.CreateLeague(9);
        var history = TestHistoryFactory.CreateEmpty();
        var fixedPods = new List<List<string>>();

        var result = _generator.Generate(league, 3, history, "February", fixedPods);

        Assert.Equal(3, result.Pods.Count);
        MatchingAssertions.AssertValidMatchings(result, league);
    }

    [Fact]
    public void Generate_NullFixedPods_WorksNormally()
    {
        var league = TestLeagueFactory.CreateLeague(9);
        var history = TestHistoryFactory.CreateEmpty();

        var result = _generator.Generate(league, 3, history, "February", null);

        Assert.Equal(3, result.Pods.Count);
        MatchingAssertions.AssertValidMatchings(result, league);
    }

    [Fact]
    public void Generate_FixedPodsGenerateCorrectMatches()
    {
        var league = TestLeagueFactory.CreateLeague(6);
        var history = TestHistoryFactory.CreateEmpty();
        var fixedPods = new List<List<string>> { new() { "1", "2", "3" } };

        var result = _generator.Generate(league, 3, history, "February", fixedPods);

        var fixedPod = result.Pods[0];
        
        // Should have 3 matches for 3 players
        Assert.Equal(3, fixedPod.Matches.Count);
        
        // All players should play each other
        Assert.Contains(fixedPod.Matches, m => m.Involves("1") && m.Involves("2"));
        Assert.Contains(fixedPod.Matches, m => m.Involves("1") && m.Involves("3"));
        Assert.Contains(fixedPod.Matches, m => m.Involves("2") && m.Involves("3"));
    }
}
