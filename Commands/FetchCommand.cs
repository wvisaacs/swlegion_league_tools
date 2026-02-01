using System.CommandLine;
using LeagueTools.Services;

namespace LeagueTools.Commands;

public class FetchCommand : Command
{
    public FetchCommand() : base("fetch", "Fetch or refresh league data from Longshanks")
    {
        var urlArg = new Argument<string>("url", "The Longshanks event URL (e.g., https://longshanks.org/event/31823/)");
        AddArgument(urlArg);

        this.SetHandler(async (url) =>
        {
            var scraper = new LongshanksScraper();
            var storage = new JsonLeagueStorage();

            Console.WriteLine($"Fetching league data from {url}...");

            try
            {
                var league = await scraper.ScrapeLeagueAsync(url);
                storage.SaveLeague(league);

                Console.WriteLine();
                Console.WriteLine($"League: {league.Name}");
                Console.WriteLine($"Event ID: {league.EventId}");
                Console.WriteLine($"Players: {league.Players.Count}");
                Console.WriteLine();

                foreach (var player in league.Players.OrderBy(p => p.Name))
                {
                    var faction = player.Faction != null ? $" [{player.Faction}]" : "";
                    var rating = player.Rating.HasValue ? $" (Rating: {player.Rating})" : "";
                    Console.WriteLine($"  - {player.DisplayName}{faction}{rating}");
                }

                Console.WriteLine();
                Console.WriteLine($"Data saved to: {storage.GetDataDirectory()}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, urlArg);
    }
}
