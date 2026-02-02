using LeagueTools.Models;

namespace LeagueTools.Tests.Fixtures;

public static class TestLeagueFactory
{
    public static League CreateLeague(int playerCount, string eventId = "test-event")
    {
        return new League
        {
            EventId = eventId,
            Name = "Test League",
            Url = $"https://test.com/event/{eventId}",
            Players = Enumerable.Range(1, playerCount)
                .Select(i => new Player 
                { 
                    Id = i.ToString(), 
                    Name = $"Player{i}",
                    Faction = i % 2 == 0 ? "Rebels" : "Empire"
                })
                .ToList(),
            LastUpdated = DateTime.UtcNow
        };
    }

    public static League CreateLeagueWithNames(params string[] names)
    {
        return new League
        {
            EventId = "test-event",
            Name = "Test League",
            Url = "https://test.com/event/test-event",
            Players = names.Select((name, i) => new Player
            {
                Id = (i + 1).ToString(),
                Name = name
            }).ToList(),
            LastUpdated = DateTime.UtcNow
        };
    }
}
