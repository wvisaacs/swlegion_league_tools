using System.Text.Json;
using System.Text.Json.Nodes;
using LeagueTools.Models.ListParser;

namespace LeagueTools.Services.ListParser;

/// <summary>
/// Service for loading and querying the Star Wars Legion card database.
/// Uses the LegionHQ2 data.json as the primary data source.
/// </summary>
public class CardDatabase
{
    private readonly Dictionary<string, LegionCard> _cardsById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, LegionCard> _cardsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<LegionCard>> _cardsByNameMultiple = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _subtitleOverrides = new(StringComparer.OrdinalIgnoreCase);

    private bool _isLoaded;

    /// <summary>
    /// Known unit subtitles for unique characters.
    /// These are required for TTS-compatible JSON export.
    /// </summary>
    private static readonly Dictionary<string, string> KnownSubtitles = new(StringComparer.OrdinalIgnoreCase)
    {
        // Rebel Commanders
        ["Luke Skywalker"] = "Hero of the Rebellion",
        ["Leia Organa"] = "Fearless and Inventive",
        ["Han Solo"] = "Unorthodox General",
        ["Jyn Erso"] = "Stardust",
        ["Cassian Andor"] = "Capable Intelligence Agent",
        ["Lando Calrissian"] = "Smooth Talking Scoundrel",
        
        // Rebel Operatives
        ["Chewbacca"] = "Walking Carpet",
        ["Sabine Wren"] = "Explosive Artist",
        ["R2-D2"] = "Hero of a Thousand Devices",
        ["C-3PO"] = "Human Cyborg Relations",
        
        // Imperial Commanders
        ["Darth Vader"] = "Dark Lord of the Sith",
        ["Emperor Palpatine"] = "Ruler of the Galactic Empire",
        ["General Veers"] = "Master Tactician",
        ["Director Orson Krennic"] = "Architect of Terror",
        ["Grand Moff Tarkin"] = "Imperial High Command",
        ["Iden Versio"] = "Inferno Squad Leader",
        
        // Imperial Operatives
        ["Boba Fett"] = "Infamous Bounty Hunter",
        ["Bossk"] = "Trandoshan Terror",
        ["Agent Kallus"] = "Hunter of the Rebellion",
        
        // Republic Commanders
        ["Obi-Wan Kenobi"] = "Civilized Warrior",
        ["Anakin Skywalker"] = "The Chosen One",
        ["Clone Captain Rex"] = "Honorable Soldier",
        ["Padmé Amidala"] = "Spirited Senator",
        ["Yoda"] = "Grand Master of the Jedi Order",
        
        // Separatist Commanders
        ["Count Dooku"] = "Darth Tyranus",
        ["General Grievous"] = "Sinister Cyborg",
        ["Maul"] = "A Rival",
        ["Cad Bane"] = "Infamous Bounty Hunter",
        
        // Additional characters - add more as needed
        ["Wicket"] = "Hero of Bright Tree",
        ["Imperial Agent"] = "Bringing Order to the Galaxy",
        ["Rebel Agent"] = "Defender of Democracy",
    };

    /// <summary>
    /// Loads the card database from the specified JSON file.
    /// </summary>
    public async Task LoadAsync(string dataFilePath)
    {
        if (_isLoaded)
            return;

        var json = await File.ReadAllTextAsync(dataFilePath);
        var document = JsonNode.Parse(json);
        
        var allCards = document?["allCards"]?.AsObject();
        if (allCards == null)
            throw new InvalidOperationException("Invalid card database format: missing 'allCards' property");

        foreach (var (id, cardNode) in allCards)
        {
            if (cardNode == null) continue;

            var card = new LegionCard
            {
                Id = id,
                CardName = cardNode["cardName"]?.GetValue<string>() ?? string.Empty,
                DisplayName = cardNode["displayName"]?.GetValue<string>(),
                CardType = cardNode["cardType"]?.GetValue<string>() ?? string.Empty,
                CardSubtype = cardNode["cardSubtype"]?.GetValue<string>(),
                Faction = cardNode["faction"]?.GetValue<string>(),
                Rank = cardNode["rank"]?.GetValue<string>(),
                Cost = cardNode["cost"]?.GetValue<int>() ?? 0,
                IsUnique = cardNode["isUnique"]?.GetValue<bool>() ?? false,
            };

            // Try to get keywords
            if (cardNode["keywords"] is JsonArray keywordsArray)
            {
                card.Keywords = keywordsArray
                    .Select(k => k?.GetValue<string>())
                    .Where(k => k != null)
                    .Cast<string>()
                    .ToList();
            }

            // Try to get upgrade bar
            if (cardNode["upgradeBar"] is JsonArray upgradeBarArray)
            {
                card.UpgradeBar = upgradeBarArray
                    .Select(u => u?.GetValue<string>())
                    .Where(u => u != null)
                    .Cast<string>()
                    .ToList();
            }

            // Apply known subtitle if available
            if (KnownSubtitles.TryGetValue(card.CardName, out var subtitle))
            {
                card.Subtitle = subtitle;
            }

            _cardsById[id] = card;

            // Index by card name (handle duplicates - e.g., multiple Luke Skywalker versions)
            if (!_cardsByName.ContainsKey(card.CardName))
            {
                _cardsByName[card.CardName] = card;
            }

            if (!_cardsByNameMultiple.TryGetValue(card.CardName, out var cardList))
            {
                cardList = [];
                _cardsByNameMultiple[card.CardName] = cardList;
            }
            cardList.Add(card);
        }

        _isLoaded = true;
    }

    /// <summary>
    /// Gets a card by its ID.
    /// </summary>
    public LegionCard? GetById(string id)
    {
        return _cardsById.TryGetValue(id, out var card) ? card : null;
    }

    /// <summary>
    /// Gets a card by its name. Returns the first match if multiple cards share the same name.
    /// </summary>
    public LegionCard? GetByName(string name)
    {
        // Try exact match first
        if (_cardsByName.TryGetValue(name, out var card))
            return card;

        // Try partial match (for abbreviated names in text lists)
        var partialMatch = _cardsByName.Values
            .FirstOrDefault(c => c.CardName.StartsWith(name, StringComparison.OrdinalIgnoreCase));

        return partialMatch;
    }

    /// <summary>
    /// Gets all cards with the specified name (e.g., all Luke Skywalker variants).
    /// </summary>
    public IReadOnlyList<LegionCard> GetAllByName(string name)
    {
        return _cardsByNameMultiple.TryGetValue(name, out var cards) 
            ? cards 
            : [];
    }

    /// <summary>
    /// Gets the card ID for a given card name.
    /// </summary>
    public string? GetIdByName(string name)
    {
        return GetByName(name)?.Id;
    }

    /// <summary>
    /// Gets all units of the specified faction.
    /// </summary>
    public IEnumerable<LegionCard> GetUnitsByFaction(string faction)
    {
        return _cardsById.Values
            .Where(c => c.CardType == "unit" && 
                       string.Equals(c.Faction, faction, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Detects the faction based on a unit name.
    /// </summary>
    public string? DetectFactionFromUnit(string unitName)
    {
        var card = GetByName(unitName);
        return card?.Faction;
    }

    /// <summary>
    /// Gets all command cards.
    /// </summary>
    public IEnumerable<LegionCard> GetCommandCards()
    {
        return _cardsById.Values.Where(c => c.CardType == "command");
    }

    /// <summary>
    /// Gets all upgrade cards.
    /// </summary>
    public IEnumerable<LegionCard> GetUpgradeCards()
    {
        return _cardsById.Values.Where(c => c.CardType == "upgrade");
    }

    /// <summary>
    /// Whether the database has been loaded.
    /// </summary>
    public bool IsLoaded => _isLoaded;

    /// <summary>
    /// Total number of cards in the database.
    /// </summary>
    public int Count => _cardsById.Count;
}
