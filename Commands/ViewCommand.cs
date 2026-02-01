using System.CommandLine;
using LeagueTools.Services;

namespace LeagueTools.Commands;

public class ViewCommand : Command
{
    public ViewCommand() : base("view", "View league data or matchings")
    {
        var eventIdArg = new Argument<string>("event-id", "The Longshanks event ID");
        var monthOption = new Option<string?>("--month", "View matchings for a specific month");

        AddArgument(eventIdArg);
        AddOption(monthOption);

        this.SetHandler((eventId, month) =>
        {
            var storage = new JsonLeagueStorage();

            var league = storage.LoadLeague(eventId);
            if (league == null)
            {
                Console.Error.WriteLine($"Error: League {eventId} not found. Run 'fetch' first.");
                Environment.Exit(1);
                return;
            }

            Console.WriteLine($"League: {league.Name}");
            Console.WriteLine($"Event ID: {league.EventId}");
            Console.WriteLine($"URL: {league.Url}");
            Console.WriteLine($"Last Updated: {league.LastUpdated:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine($"Players: {league.Players.Count}");
            Console.WriteLine();

            if (string.IsNullOrEmpty(month))
            {
                // Show players
                Console.WriteLine("Registered Players:");
                foreach (var player in league.Players.OrderBy(p => p.Name))
                {
                    var faction = player.Faction != null ? $" [{player.Faction}]" : "";
                    var rating = player.Rating.HasValue ? $" (Rating: {player.Rating})" : "";
                    Console.WriteLine($"  - {player.DisplayName}{faction}{rating}");
                }
            }
            else
            {
                // Show matchings for month
                var matchings = storage.LoadMatchings(eventId, month);
                if (matchings == null)
                {
                    Console.Error.WriteLine($"No matchings found for {month}. Run 'generate' first.");
                    Environment.Exit(1);
                    return;
                }

                Console.WriteLine($"Matchings for {matchings.Month}:");
                Console.WriteLine($"Generated: {matchings.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
                Console.WriteLine($"Target Pod Size: {matchings.TargetPodSize}");
                Console.WriteLine($"Total Matches: {matchings.TotalMatches}");
                Console.WriteLine();

                foreach (var pod in matchings.Pods)
                {
                    var overflow = pod.IsOverflow ? " (overflow)" : "";
                    Console.WriteLine($"Pod {pod.PodId}{overflow}:");

                    foreach (var playerId in pod.PlayerIds)
                    {
                        var player = league.Players.FirstOrDefault(p => p.Id == playerId);
                        Console.WriteLine($"    {player?.DisplayName ?? playerId}");
                    }

                    Console.WriteLine("  Matches:");
                    foreach (var match in pod.Matches)
                    {
                        Console.WriteLine($"    - {match}");
                    }
                    Console.WriteLine();
                }
            }
        }, eventIdArg, monthOption);
    }
}
