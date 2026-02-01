using LeagueTools.Models;
using LeagueTools.Tests.Fixtures;

namespace LeagueTools.Tests.Unit;

public class MatchHistoryTests
{
    [Fact]
    public void HavePlayed_WhenPlayersHavePlayed_ReturnsTrue()
    {
        var history = TestHistoryFactory.CreateWithPairings("test",
            ("1", "2", "February"));

        Assert.True(history.HavePlayed("1", "2"));
        Assert.True(history.HavePlayed("2", "1")); // Order shouldn't matter
    }

    [Fact]
    public void HavePlayed_WhenPlayersHaveNotPlayed_ReturnsFalse()
    {
        var history = TestHistoryFactory.CreateWithPairings("test",
            ("1", "2", "February"));

        Assert.False(history.HavePlayed("1", "3"));
        Assert.False(history.HavePlayed("3", "4"));
    }

    [Fact]
    public void HavePlayed_EmptyHistory_ReturnsFalse()
    {
        var history = TestHistoryFactory.CreateEmpty();

        Assert.False(history.HavePlayed("1", "2"));
    }

    [Fact]
    public void TimesPlayed_CountsCorrectly()
    {
        var history = TestHistoryFactory.CreateWithPairings("test",
            ("1", "2", "February"),
            ("1", "2", "March"),
            ("1", "3", "February"));

        Assert.Equal(2, history.TimesPlayed("1", "2"));
        Assert.Equal(2, history.TimesPlayed("2", "1")); // Order shouldn't matter
        Assert.Equal(1, history.TimesPlayed("1", "3"));
        Assert.Equal(0, history.TimesPlayed("2", "3"));
    }

    [Fact]
    public void GetPreviousOpponents_ReturnsAllOpponents()
    {
        var history = TestHistoryFactory.CreateWithPairings("test",
            ("1", "2", "February"),
            ("1", "3", "February"),
            ("1", "4", "March"));

        var opponents = history.GetPreviousOpponents("1");

        Assert.Equal(3, opponents.Count);
        Assert.Contains("2", opponents);
        Assert.Contains("3", opponents);
        Assert.Contains("4", opponents);
    }

    [Fact]
    public void GetPreviousOpponents_ReturnsDistinct()
    {
        var history = TestHistoryFactory.CreateWithPairings("test",
            ("1", "2", "February"),
            ("1", "2", "March")); // Same opponent twice

        var opponents = history.GetPreviousOpponents("1");

        Assert.Single(opponents);
        Assert.Contains("2", opponents);
    }

    [Fact]
    public void GetPreviousOpponents_NoMatches_ReturnsEmpty()
    {
        var history = TestHistoryFactory.CreateWithPairings("test",
            ("2", "3", "February"));

        var opponents = history.GetPreviousOpponents("1");

        Assert.Empty(opponents);
    }

    [Fact]
    public void AddPairing_AddsPairingCorrectly()
    {
        var history = TestHistoryFactory.CreateEmpty();

        history.AddPairing("1", "2", "February");

        Assert.Single(history.Pairings);
        Assert.True(history.HavePlayed("1", "2"));
    }

    [Fact]
    public void IsPairing_MatchesRegardlessOfOrder()
    {
        var pairing = new HistoricalPairing
        {
            Player1Id = "1",
            Player2Id = "2",
            Month = "February"
        };

        Assert.True(pairing.IsPairing("1", "2"));
        Assert.True(pairing.IsPairing("2", "1"));
        Assert.False(pairing.IsPairing("1", "3"));
    }
}
