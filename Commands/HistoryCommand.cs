using System.CommandLine;
using LeagueTools.Services;

namespace LeagueTools.Commands;

public class HistoryCommand : Command
{
    public HistoryCommand() : base("history", "View match history for a league")
    {
        var eventIdArg = new Argument<string>("event-id", "The Longshanks event ID");
        var playerOption = new Option<string?>("--player", "Filter history for a specific player ID");

        AddArgument(eventIdArg);
        AddOption(playerOption);

        this.SetHandler((eventId, playerId) =>
        {
            var storage = new JsonLeagueStorage();
            var historyService = new JsonHistoryService();

            var league = storage.LoadLeague(eventId);
            if (league == null)
            {
                Console.Error.WriteLine($"Error: League {eventId} not found. Run 'fetch' first.");
                Environment.Exit(1);
                return;
            }

            var history = historyService.LoadHistory(eventId);

            Console.WriteLine($"Match History: {league.Name}");
            Console.WriteLine($"Total Recorded Pairings: {history.Pairings.Count}");
            Console.WriteLine();

            var playerLookup = league.Players.ToDictionary(p => p.Id);

            if (string.IsNullOrEmpty(playerId))
            {
                // Show all history grouped by month
                var byMonth = history.Pairings.GroupBy(p => p.Month).OrderBy(g => g.Key);

                foreach (var monthGroup in byMonth)
                {
                    Console.WriteLine($"{monthGroup.Key}:");
                    foreach (var pairing in monthGroup)
                    {
                        var p1Name = playerLookup.TryGetValue(pairing.Player1Id, out var p1) ? p1.DisplayName : pairing.Player1Id;
                        var p2Name = playerLookup.TryGetValue(pairing.Player2Id, out var p2) ? p2.DisplayName : pairing.Player2Id;
                        Console.WriteLine($"  - {p1Name} vs {p2Name}");
                    }
                    Console.WriteLine();
                }
            }
            else
            {
                // Show history for specific player
                var player = league.Players.FirstOrDefault(p => p.Id == playerId);
                var playerName = player?.DisplayName ?? playerId;

                Console.WriteLine($"History for {playerName}:");

                var opponents = history.GetPreviousOpponents(playerId);
                if (opponents.Count == 0)
                {
                    Console.WriteLine("  No matches recorded.");
                }
                else
                {
                    Console.WriteLine($"  Previous Opponents ({opponents.Count}):");
                    foreach (var oppId in opponents)
                    {
                        var oppName = playerLookup.TryGetValue(oppId, out var opp) ? opp.DisplayName : oppId;
                        var times = history.TimesPlayed(playerId, oppId);
                        var timesStr = times > 1 ? $" (x{times})" : "";
                        Console.WriteLine($"    - {oppName}{timesStr}");
                    }
                }
            }
        }, eventIdArg, playerOption);
    }
}
