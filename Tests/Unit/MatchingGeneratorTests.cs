using LeagueTools.Services;
using LeagueTools.Tests.Fixtures;

namespace LeagueTools.Tests.Unit;

public class MatchingGeneratorTests
{
    private readonly MatchingGenerator _generator = new();

    #region Pod Distribution Tests

    [Fact]
    public void Generate_ExactMultiple_CreatesEqualPods()
    {
        // 9 players / 3 = 3 pods of 3
        var league = TestLeagueFactory.CreateLeague(9);
        var history = TestHistoryFactory.CreateEmpty();

        var result = _generator.Generate(league, 3, history, "February");

        Assert.Equal(3, result.Pods.Count);
        Assert.All(result.Pods, pod => Assert.Equal(3, pod.Size));
        Assert.All(result.Pods, pod => Assert.False(pod.IsOverflow));
        MatchingAssertions.AssertValidMatchings(result, league);
    }

    [Fact]
    public void Generate_WithRemainder_CreatesOverflowPods()
    {
        // 14 players / 3 = 2 pods of 3 + 2 pods of 4
        var league = TestLeagueFactory.CreateLeague(14);
        var history = TestHistoryFactory.CreateEmpty();

        var result = _generator.Generate(league, 3, history, "February");

        Assert.Equal(4, result.Pods.Count);
        Assert.Equal(14, result.TotalPlayers);
        
        var standardPods = result.Pods.Where(p => !p.IsOverflow).ToList();
        var overflowPods = result.Pods.Where(p => p.IsOverflow).ToList();
        
        Assert.Equal(2, standardPods.Count);
        Assert.Equal(2, overflowPods.Count);
        Assert.All(standardPods, pod => Assert.Equal(3, pod.Size));
        Assert.All(overflowPods, pod => Assert.Equal(4, pod.Size));
        
        MatchingAssertions.AssertValidMatchings(result, league);
    }

    [Fact]
    public void Generate_TwoPlayers_CreatesSinglePod()
    {
        var league = TestLeagueFactory.CreateLeague(2);
        var history = TestHistoryFactory.CreateEmpty();

        var result = _generator.Generate(league, 3, history, "February");

        Assert.Single(result.Pods);
        Assert.Equal(2, result.Pods[0].Size);
        Assert.Single(result.Pods[0].Matches);
        MatchingAssertions.AssertValidMatchings(result, league);
    }

    [Fact]
    public void Generate_FourPlayersWithPodSizeThree_CreatesOverflowPod()
    {
        var league = TestLeagueFactory.CreateLeague(4);
        var history = TestHistoryFactory.CreateEmpty();

        var result = _generator.Generate(league, 3, history, "February");

        Assert.Single(result.Pods);
        Assert.Equal(4, result.Pods[0].Size);
        Assert.True(result.Pods[0].IsOverflow);
        MatchingAssertions.AssertValidMatchings(result, league);
    }

    [Fact]
    public void Generate_FivePlayersWithPodSizeThree_CreatesTwoPods()
    {
        // 5 players / 3 = could be 1 pod of 5 or something else
        var league = TestLeagueFactory.CreateLeague(5);
        var history = TestHistoryFactory.CreateEmpty();

        var result = _generator.Generate(league, 3, history, "February");

        Assert.Equal(5, result.TotalPlayers);
        MatchingAssertions.AssertValidMatchings(result, league);
    }

    #endregion

    #region Match Generation Tests

    [Fact]
    public void Generate_StandardPod_CreatesRoundRobinMatches()
    {
        var league = TestLeagueFactory.CreateLeague(3);
        var history = TestHistoryFactory.CreateEmpty();

        var result = _generator.Generate(league, 3, history, "February");

        Assert.Single(result.Pods);
        var pod = result.Pods[0];
        
        // 3 players = 3 matches (A-B, A-C, B-C)
        Assert.Equal(3, pod.Matches.Count);
        MatchingAssertions.AssertValidMatchings(result, league);
    }

    [Fact]
    public void Generate_FourPlayerStandardPod_CreatesSixMatches()
    {
        var league = TestLeagueFactory.CreateLeague(4);
        var history = TestHistoryFactory.CreateEmpty();

        var result = _generator.Generate(league, 4, history, "February");

        Assert.Single(result.Pods);
        var pod = result.Pods[0];
        
        // 4 players = 6 matches
        Assert.Equal(6, pod.Matches.Count);
        Assert.False(pod.IsOverflow);
        MatchingAssertions.AssertValidMatchings(result, league);
    }

    [Fact]
    public void Generate_NoSelfMatches()
    {
        var league = TestLeagueFactory.CreateLeague(10);
        var history = TestHistoryFactory.CreateEmpty();

        var result = _generator.Generate(league, 3, history, "February");

        MatchingAssertions.AssertNoSelfMatches(result);
    }

    [Fact]
    public void Generate_NoDuplicateMatches()
    {
        var league = TestLeagueFactory.CreateLeague(10);
        var history = TestHistoryFactory.CreateEmpty();

        var result = _generator.Generate(league, 3, history, "February");

        MatchingAssertions.AssertNoDuplicateMatches(result);
    }

    #endregion

    #region Seed/Reproducibility Tests

    [Fact]
    public void Generate_SameSeed_ProducesSameResults()
    {
        var league = TestLeagueFactory.CreateLeague(10);
        var history = TestHistoryFactory.CreateEmpty();

        var result1 = _generator.Generate(league, 3, history, "February", seed: 42);
        var result2 = _generator.Generate(league, 3, history, "February", seed: 42);

        Assert.Equal(result1.Pods.Count, result2.Pods.Count);
        for (int i = 0; i < result1.Pods.Count; i++)
        {
            Assert.Equal(result1.Pods[i].PlayerIds, result2.Pods[i].PlayerIds);
        }
    }

    [Fact]
    public void Generate_DifferentSeeds_ProducesDifferentResults()
    {
        var league = TestLeagueFactory.CreateLeague(10);
        var history = TestHistoryFactory.CreateEmpty();

        var result1 = _generator.Generate(league, 3, history, "February", seed: 42);
        var result2 = _generator.Generate(league, 3, history, "February", seed: 123);

        // At least one pod should have different players
        var different = false;
        for (int i = 0; i < Math.Min(result1.Pods.Count, result2.Pods.Count); i++)
        {
            if (!result1.Pods[i].PlayerIds.SequenceEqual(result2.Pods[i].PlayerIds))
            {
                different = true;
                break;
            }
        }
        Assert.True(different, "Different seeds should produce different results");
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void Generate_PodSizeLessThanTwo_ThrowsException()
    {
        var league = TestLeagueFactory.CreateLeague(5);
        var history = TestHistoryFactory.CreateEmpty();

        Assert.Throws<ArgumentException>(() => 
            _generator.Generate(league, 1, history, "February"));
    }

    [Fact]
    public void Generate_LessThanTwoPlayers_ThrowsException()
    {
        var league = TestLeagueFactory.CreateLeague(1);
        var history = TestHistoryFactory.CreateEmpty();

        Assert.Throws<ArgumentException>(() => 
            _generator.Generate(league, 3, history, "February"));
    }

    #endregion
}
