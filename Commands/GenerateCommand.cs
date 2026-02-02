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
        var fixedPodOption = new Option<string[]>(
            "--fixed-pod", 
            "Specify a fixed pod as comma-separated player IDs (can be used multiple times)")
        { 
            AllowMultipleArgumentsPerToken = true 
        };

        AddArgument(eventIdArg);
        AddOption(monthOption);
        AddOption(podSizeOption);
        AddOption(seedOption);
        AddOption(fixedPodOption);

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

            // Parse fixed pods from comma-separated strings
            var fixedPods = ParseFixedPods(fixedPodStrings, league);

            Console.WriteLine($"Generating matchings for {league.Name}...");
            Console.WriteLine($"  Month: {month}");
            Console.WriteLine($"  Pod Size: {podSize}");
            Console.WriteLine($"  Players: {league.Players.Count}");
            if (fixedPods.Count > 0)
            {
                Console.WriteLine($"  Fixed Pods: {fixedPods.Count}");
            }
            Console.WriteLine();

            try
            {
                var matchings = generator.Generate(league, podSize, history, month, fixedPods, seed);
                storage.SaveMatchings(matchings);
                historyService.RecordMatches(eventId, matchings);

                PrintMatchings(matchings, league, fixedPods.Count);

                Console.WriteLine();
                Console.WriteLine($"Matchings saved to: {storage.GetDataDirectory()}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, eventIdArg, monthOption, podSizeOption, seedOption, fixedPodOption);
    }

    private static List<List<string>> ParseFixedPods(string[] fixedPodStrings, LeagueTools.Models.League league)
    {
        var fixedPods = new List<List<string>>();
        if (fixedPodStrings == null || fixedPodStrings.Length == 0)
            return fixedPods;

        var playerLookup = league.Players.ToDictionary(p => p.Id);
        var playerNameLookup = league.Players.ToDictionary(p => p.Name.ToLowerInvariant(), p => p.Id);

        foreach (var podString in fixedPodStrings)
        {
            var playerIds = new List<string>();
            var parts = podString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var part in parts)
            {
                // Try as ID first
                if (playerLookup.ContainsKey(part))
                {
                    playerIds.Add(part);
                }
                // Try as name (case-insensitive)
                else if (playerNameLookup.TryGetValue(part.ToLowerInvariant(), out var id))
                {
                    playerIds.Add(id);
                }
                else
                {
                    throw new ArgumentException($"Player '{part}' not found in league. Use player ID or exact name.");
                }
            }

            if (playerIds.Count > 0)
            {
                fixedPods.Add(playerIds);
            }
        }

        return fixedPods;
    }

    private static void PrintMatchings(LeagueTools.Models.MonthlyMatchings matchings, LeagueTools.Models.League league, int fixedPodCount)
    {
        var playerLookup = league.Players.ToDictionary(p => p.Id);

        Console.WriteLine($"Generated {matchings.Pods.Count} pods with {matchings.TotalMatches} total matches:");
        Console.WriteLine();

        var podIndex = 0;
        foreach (var pod in matchings.Pods)
        {
            var overflow = pod.IsOverflow ? " (overflow)" : "";
            var isFixed = podIndex < fixedPodCount ? " [FIXED]" : "";
            Console.WriteLine($"Pod {pod.PodId}{overflow}{isFixed} - {pod.Size} players:");

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
            podIndex++;
        }
    }
}
