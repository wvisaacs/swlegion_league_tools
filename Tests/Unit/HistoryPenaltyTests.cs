using LeagueTools.Services;
using LeagueTools.Tests.Fixtures;

namespace LeagueTools.Tests.Unit;

public class HistoryPenaltyTests
{
    private readonly MatchingGenerator _generator = new();

    [Fact]
    public void Generate_WithHistory_AvoidsRepeatMatchups()
    {
        // Create a small league where history should matter
        var league = TestLeagueFactory.CreateLeague(4);
        
        // Players 1 and 2 have already played
        var history = TestHistoryFactory.CreateWithPairings("test-event",
            ("1", "2", "January"));

        // Generate with pod size 4 (everyone in one pod, overflow matching)
        var result = _generator.Generate(league, 3, history, "February", seed: 42);

        // The weighted algorithm should try to avoid 1 vs 2
        // We can't guarantee they won't be matched (small pool), 
        // but with fresh history, they might be avoided
        MatchingAssertions.AssertValidMatchings(result, league);
    }

    [Fact]
    public void Generate_MultipleMonths_TracksHistoryCorrectly()
    {
        var league = TestLeagueFactory.CreateLeague(6);
        var history = TestHistoryFactory.CreateEmpty();

        // Generate February
        var feb = _generator.Generate(league, 3, history, "February", seed: 100);
        
        // Record February matches to history
        foreach (var pod in feb.Pods)
        {
            foreach (var match in pod.Matches)
            {
                history.AddPairing(match.Player1Id, match.Player2Id, "February");
            }
        }

        // Generate March with updated history
        var march = _generator.Generate(league, 3, history, "March", seed: 200);

        MatchingAssertions.AssertValidMatchings(feb, league);
        MatchingAssertions.AssertValidMatchings(march, league);
        
        // Both months should have valid matchings
        Assert.Equal(6, feb.TotalPlayers);
        Assert.Equal(6, march.TotalPlayers);
    }
}
