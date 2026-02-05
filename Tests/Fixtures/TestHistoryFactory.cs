using LeagueTools.Models;

namespace LeagueTools.Tests.Fixtures;

public static class TestHistoryFactory
{
    public static MatchHistory CreateEmpty(string eventId = "test-event")
    {
        return new MatchHistory { EventId = eventId };
    }

    public static MatchHistory CreateWithPairings(string eventId, params (string p1, string p2, string month)[] pairings)
    {
        var history = new MatchHistory { EventId = eventId };
        foreach (var (p1, p2, month) in pairings)
        {
            history.AddPairing(p1, p2, month);
        }
        return history;
    }
}
