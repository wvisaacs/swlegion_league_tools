using System.CommandLine;
using LeagueTools.Services;

namespace LeagueTools.Commands;

public class SwapCommand : Command
{
    public SwapCommand() : base("swap", "Swap two players between pods")
    {
        var eventIdArg = new Argument<string>("event-id", "The Longshanks event ID");
        var monthOption = new Option<string>("--month", "The month to modify") { IsRequired = true };
        var player1Option = new Option<string>("--player1", "First player ID to swap") { IsRequired = true };
        var player2Option = new Option<string>("--player2", "Second player ID to swap") { IsRequired = true };

        AddArgument(eventIdArg);
        AddOption(monthOption);
        AddOption(player1Option);
        AddOption(player2Option);

        this.SetHandler((eventId, month, player1Id, player2Id) =>
        {
            var storage = new JsonLeagueStorage();
            var historyService = new JsonHistoryService();
            var generator = new MatchingGenerator();

            var league = storage.LoadLeague(eventId);
            if (league == null)
            {
                Console.Error.WriteLine($"Error: League {eventId} not found.");
                Environment.Exit(1);
                return;
            }

            var matchings = storage.LoadMatchings(eventId, month);
            if (matchings == null)
            {
                Console.Error.WriteLine($"Error: No matchings found for {month}.");
                Environment.Exit(1);
                return;
            }

            // Find the pods containing each player
            var pod1 = matchings.Pods.FirstOrDefault(p => p.PlayerIds.Contains(player1Id));
            var pod2 = matchings.Pods.FirstOrDefault(p => p.PlayerIds.Contains(player2Id));

            if (pod1 == null)
            {
                Console.Error.WriteLine($"Error: Player {player1Id} not found in any pod.");
                Environment.Exit(1);
                return;
            }

            if (pod2 == null)
            {
                Console.Error.WriteLine($"Error: Player {player2Id} not found in any pod.");
                Environment.Exit(1);
                return;
            }

            if (pod1.PodId == pod2.PodId)
            {
                Console.Error.WriteLine("Error: Both players are in the same pod. Nothing to swap.");
                Environment.Exit(1);
                return;
            }

            // Swap the players
            pod1.PlayerIds.Remove(player1Id);
            pod1.PlayerIds.Add(player2Id);
            pod2.PlayerIds.Remove(player2Id);
            pod2.PlayerIds.Add(player1Id);

            // Regenerate matches for affected pods
            var history = historyService.LoadHistory(eventId);
            var playerLookup = league.Players.ToDictionary(p => p.Id);

            RegenerateMatches(pod1, playerLookup, history, matchings.TargetPodSize);
            RegenerateMatches(pod2, playerLookup, history, matchings.TargetPodSize);

            // Save updated matchings
            storage.SaveMatchings(matchings);

            var p1Name = playerLookup.TryGetValue(player1Id, out var p1) ? p1.DisplayName : player1Id;
            var p2Name = playerLookup.TryGetValue(player2Id, out var p2) ? p2.DisplayName : player2Id;

            Console.WriteLine($"Swapped {p1Name} (now in Pod {pod2.PodId}) and {p2Name} (now in Pod {pod1.PodId})");
            Console.WriteLine("Matches regenerated for affected pods.");
        }, eventIdArg, monthOption, player1Option, player2Option);
    }

    private static void RegenerateMatches(
        LeagueTools.Models.Pod pod,
        Dictionary<string, LeagueTools.Models.Player> playerLookup,
        LeagueTools.Models.MatchHistory history,
        int targetPodSize)
    {
        pod.Matches.Clear();
        var n = pod.PlayerIds.Count;

        // Full round-robin for standard pods, or if it's close to target
        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                var p1Id = pod.PlayerIds[i];
                var p2Id = pod.PlayerIds[j];

                if (playerLookup.TryGetValue(p1Id, out var p1) && playerLookup.TryGetValue(p2Id, out var p2))
                {
                    pod.Matches.Add(new LeagueTools.Models.Match
                    {
                        Player1Id = p1.Id,
                        Player2Id = p2.Id,
                        Player1Name = p1.DisplayName,
                        Player2Name = p2.DisplayName
                    });
                }
            }
        }

        pod.IsOverflow = n > targetPodSize;
    }
}
