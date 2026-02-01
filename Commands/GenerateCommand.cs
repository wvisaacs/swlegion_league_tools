using System.CommandLine;
using LeagueTools.Services;

namespace LeagueTools.Commands;

public class GenerateCommand : Command
{
    public GenerateCommand() : base("generate", "Generate monthly matchings for a league")
    {
        var eventIdArg = new Argument<string>("event-id", "The Longshanks event ID");
        var monthOption = new Option<string>("--month", "The month name (e.g., February)") { IsRequired = true };
        var podSizeOption = new Option<int>("--pod-size", () => 3, "Target pod size (default: 3)");
        var seedOption = new Option<int?>("--seed", "Random seed for reproducible results");

        AddArgument(eventIdArg);
        AddOption(monthOption);
        AddOption(podSizeOption);
        AddOption(seedOption);

        this.SetHandler((eventId, month, podSize, seed) =>
        {
            var storage = new JsonLeagueStorage();
            var historyService = new JsonHistoryService();
            var generator = new MatchingGenerator();

            var league = storage.LoadLeague(eventId);
            if (league == null)
            {
                Console.Error.WriteLine($"Error: League {eventId} not found. Run 'fetch' first.");
                Environment.Exit(1);
                return;
            }

            var history = historyService.LoadHistory(eventId);

            Console.WriteLine($"Generating matchings for {league.Name}...");
            Console.WriteLine($"  Month: {month}");
            Console.WriteLine($"  Pod Size: {podSize}");
            Console.WriteLine($"  Players: {league.Players.Count}");
            Console.WriteLine();

            try
            {
                var matchings = generator.Generate(league, podSize, history, month, seed);
                storage.SaveMatchings(matchings);
                historyService.RecordMatches(eventId, matchings);

                PrintMatchings(matchings, league);

                Console.WriteLine();
                Console.WriteLine($"Matchings saved to: {storage.GetDataDirectory()}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, eventIdArg, monthOption, podSizeOption, seedOption);
    }

    private static void PrintMatchings(LeagueTools.Models.MonthlyMatchings matchings, LeagueTools.Models.League league)
    {
        var playerLookup = league.Players.ToDictionary(p => p.Id);

        Console.WriteLine($"Generated {matchings.Pods.Count} pods with {matchings.TotalMatches} total matches:");
        Console.WriteLine();

        foreach (var pod in matchings.Pods)
        {
            var overflow = pod.IsOverflow ? " (overflow)" : "";
            Console.WriteLine($"Pod {pod.PodId}{overflow} - {pod.Size} players:");

            foreach (var playerId in pod.PlayerIds)
            {
                if (playerLookup.TryGetValue(playerId, out var player))
                {
                    Console.WriteLine($"    {player.DisplayName}");
                }
            }

            Console.WriteLine("  Matches:");
            foreach (var match in pod.Matches)
            {
                Console.WriteLine($"    - {match}");
            }
            Console.WriteLine();
        }
    }
}
