using LeagueTools.Models;

namespace LeagueTools.Services;

public interface ILeagueScraper
{
    Task<League> ScrapeLeagueAsync(string eventUrl, CancellationToken ct = default);
}
