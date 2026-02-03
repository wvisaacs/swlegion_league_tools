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
        var fixedPodsOption = new Option<string[]>("--fixed-pod", "Fixed pod player names (comma-separated). Can be specified multiple times.") { AllowMultipleArgumentsPerToken = true };

        AddArgument(eventIdArg);
        AddOption(monthOption);
        AddOption(podSizeOption);
        AddOption(seedOption);
        AddOption(fixedPodsOption);

        this.SetHandler((eventId, month, podSize, seed, fixedPodStrings) =>
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

            // Parse fixed pods
            List<List<string>>? fixedPods = null;
            if (fixedPodStrings != null && fixedPodStrings.Length > 0)
            {
                fixedPods = new List<List<string>>();
                foreach (var podString in fixedPodStrings)
                {
                    var names = podString.Split(',').Select(n => n.Trim()).ToList();
                    var playerIds = new List<string>();
                    foreach (var name in names)
                    {
                        var player = league.Players.FirstOrDefault(p => 
                            p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                            p.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase));
                        if (player == null)
                        {
                            Console.Error.WriteLine($"Error: Player '{name}' not found in league.");
                            Environment.Exit(1);
                            return;
                        }
                        playerIds.Add(player.Id);
                    }
                    fixedPods.Add(playerIds);
                }
            }

            Console.WriteLine($"Generating matchings for {league.Name}...");
            Console.WriteLine($"  Month: {month}");
            Console.WriteLine($"  Pod Size: {podSize}");
            Console.WriteLine($"  Players: {league.Players.Count}");
            if (fixedPods != null)
            {
                Console.WriteLine($"  Fixed Pods: {fixedPods.Count}");
            }
            Console.WriteLine();

            try
            {
                var matchings = generator.Generate(league, podSize, history, month, seed, fixedPods);
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
        }, eventIdArg, monthOption, podSizeOption, seedOption, fixedPodsOption);
    }

    private static void PrintMatchings(LeagueTools.Models.MonthlyMatchings matchings, LeagueTools.Models.League league)
    {
        var playerLookup = league.Players.ToDictionary(p => p.Id);

        Console.WriteLine($"Generated {matchings.Pods.Count} pods with {matchings.TotalMatches} total matches:");
        Console.WriteLine();

        foreach (var pod in matchings.Pods)
        {
            var overflow = pod.IsOverflow ? " (overflow)" : "";
            var fixedLabel = pod.IsFixed ? " [FIXED]" : "";
            Console.WriteLine($"Pod {pod.PodId}{overflow}{fixedLabel} - {pod.Size} players:");

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
