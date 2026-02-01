using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using LeagueTools.Models;

namespace LeagueTools.Services;

public class LongshanksScraper : ILeagueScraper
{
    private const int MaxRetries = 3;
    private const int BaseDelayMs = 1000;

    public async Task<League> ScrapeLeagueAsync(string eventUrl, CancellationToken ct = default)
    {
        var eventId = ExtractEventId(eventUrl);
        if (string.IsNullOrEmpty(eventId))
            throw new ArgumentException($"Could not extract event ID from URL: {eventUrl}");

        League? league = null;
        Exception? lastException = null;

        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                league = await ScrapeWithPlaywrightAsync(eventUrl, eventId, ct);
                break;
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                lastException = ex;
                var delay = BaseDelayMs * (int)Math.Pow(2, attempt - 1);
                Console.WriteLine($"Scrape attempt {attempt} failed: {ex.Message}. Retrying in {delay}ms...");
                await Task.Delay(delay, ct);
            }
        }

        if (league == null)
            throw new Exception($"Failed to scrape league after {MaxRetries} attempts", lastException);

        return league;
    }

    private static string? ExtractEventId(string url)
    {
        var match = Regex.Match(url, @"/event/(\d+)");
        return match.Success ? match.Groups[1].Value : null;
    }

    private async Task<League> ScrapeWithPlaywrightAsync(string eventUrl, string eventId, CancellationToken ct)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var page = await browser.NewPageAsync();
        await page.GotoAsync(eventUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 30000
        });

        // Extract event name
        var eventName = await page.Locator("h1").First.TextContentAsync() ?? "Unknown League";

        // Wait for player list to load dynamically
        await Task.Delay(3000, ct);

        // Use JavaScript to extract player data
        var playerDataJson = await page.EvaluateAsync<string>(@"() => {
            const players = [];
            const seen = new Set();
            const playerElements = document.querySelectorAll('.player_disp');
            
            playerElements.forEach((el) => {
                // Skip community entries (they have .community class)
                if (el.querySelector('.community')) return;
                
                const idEl = el.querySelector('.id_number');
                if (!idEl) return;
                
                const idText = idEl.textContent.trim();
                const idMatch = idText.match(/#(\d+)/);
                if (!idMatch) return;
                
                const id = idMatch[1];
                if (seen.has(id)) return;
                seen.add(id);
                
                // Get name from either .nickname or the .player_link text
                let name = '';
                const nicknameEl = el.querySelector('.nickname');
                if (nicknameEl) {
                    name = nicknameEl.textContent.trim();
                } else {
                    const linkEl = el.querySelector('.player_link');
                    if (linkEl) {
                        name = linkEl.textContent.trim();
                    }
                }
                
                if (!name) return;
                
                // Look for faction in the parent row
                let faction = null;
                const parentRow = el.closest('div');
                if (parentRow) {
                    const grandparent = parentRow.parentElement;
                    if (grandparent) {
                        const factionImgs = grandparent.querySelectorAll('img');
                        for (const img of factionImgs) {
                            const alt = img.getAttribute('alt') || '';
                            if (alt.includes('Republic') || alt.includes('Empire') || 
                                alt.includes('Rebel') || alt.includes('Separatist') || 
                                alt.includes('Confederacy')) {
                                faction = alt;
                                break;
                            }
                        }
                    }
                }
                
                players.push({
                    id: id,
                    name: name,
                    faction: faction
                });
            });
            
            return JSON.stringify(players);
        }");

        var players = new List<Player>();
        if (!string.IsNullOrEmpty(playerDataJson))
        {
            var options = new System.Text.Json.JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            };
            var playerData = System.Text.Json.JsonSerializer.Deserialize<List<PlayerScrapedData>>(playerDataJson, options);
            if (playerData != null)
            {
                players = playerData.Select(p => new Player
                {
                    Id = p.Id,
                    Name = p.Name,
                    Faction = p.Faction
                }).ToList();
            }
        }

        return new League
        {
            EventId = eventId,
            Name = eventName.Trim(),
            Url = eventUrl,
            Players = players,
            LastUpdated = DateTime.UtcNow
        };
    }

    private class PlayerScrapedData
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Faction { get; set; }
    }
}
