using System.Text.Json;
using System.Text.Json.Serialization;
using LeagueTools.Models.ListParser;

namespace LeagueTools.Services.ListParser;

/// <summary>
/// Exports lists in LegionHQ2-compatible JSON format and generates LegionHQ2 URLs.
/// </summary>
public class LegionHqExporter : IListExporter
{
    private readonly CardDatabase _cardDatabase;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions JsonOptionsCompact = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public LegionHqExporter(CardDatabase cardDatabase)
    {
        _cardDatabase = cardDatabase;
    }

    public string ExportToJson(ParsedList list, bool indented = true)
    {
        var exportModel = new LegionHqJsonModel
        {
            Author = "Legion HQ",
            ListName = list.ListName,
            Points = list.Points,
            NumActivations = list.NumActivations,
            ArmyFaction = list.ArmyFaction,
            CommandCards = list.CommandCards,
            Contingencies = list.Contingencies,
            Units = list.Units.Select(u => new LegionHqUnitModel
            {
                Name = u.Name,
                Upgrades = u.Upgrades
            }).ToList(),
            BattlefieldDeck = new LegionHqBattlefieldModel
            {
                Scenario = list.BattlefieldDeck.Scenario,
                Conditions = list.BattlefieldDeck.Conditions,
                Deployment = list.BattlefieldDeck.Deployment,
                Objective = list.BattlefieldDeck.Objective
            }
        };

        // Generate URL if possible
        var url = GenerateUrl(list);
        if (url != null)
        {
            exportModel.ListLink = url;
        }

        return JsonSerializer.Serialize(exportModel, indented ? JsonOptions : JsonOptionsCompact);
    }

    public string? GenerateUrl(ParsedList list)
    {
        if (string.IsNullOrEmpty(list.ArmyFaction))
            return null;

        try
        {
            var urlParts = new List<string>();

            // Add units with their upgrades
            foreach (var unit in list.Units)
            {
                var unitPart = BuildUnitUrlPart(unit);
                if (unitPart != null)
                {
                    urlParts.Add(unitPart);
                }
            }

            // Add command cards
            foreach (var command in list.CommandCards)
            {
                var commandId = _cardDatabase.GetIdByName(command);
                if (commandId != null)
                {
                    urlParts.Add(commandId);
                }
            }

            // Add battlefield deck cards
            foreach (var condition in list.BattlefieldDeck.Conditions)
            {
                var cardId = _cardDatabase.GetIdByName(condition);
                if (cardId != null)
                    urlParts.Add(cardId);
            }
            foreach (var deployment in list.BattlefieldDeck.Deployment)
            {
                var cardId = _cardDatabase.GetIdByName(deployment);
                if (cardId != null)
                    urlParts.Add(cardId);
            }
            foreach (var objective in list.BattlefieldDeck.Objective)
            {
                var cardId = _cardDatabase.GetIdByName(objective);
                if (cardId != null)
                    urlParts.Add(cardId);
            }

            if (urlParts.Count == 0)
                return null;

            var encodedList = string.Join(",", urlParts);
            return $"https://legionhq2.com/list/{list.ArmyFaction}/1000:{encodedList}";
        }
        catch
        {
            return null;
        }
    }

    private string? BuildUnitUrlPart(ParsedUnit unit)
    {
        // Get the base unit name (without subtitle) for lookup
        var unitName = ExtractBaseUnitName(unit.Name);
        var unitCard = _cardDatabase.GetByName(unitName);
        if (unitCard == null)
            return null;

        // Format: 1{unitId}{upgradeId1}{upgradeId2}...
        // Use "0" for empty upgrade slots
        var parts = new List<string> { "1", unitCard.Id };

        foreach (var upgrade in unit.Upgrades)
        {
            var upgradeCard = _cardDatabase.GetByName(upgrade);
            if (upgradeCard != null)
            {
                parts.Add(upgradeCard.Id);
            }
            else
            {
                parts.Add("0"); // Unknown upgrade
            }
        }

        return string.Join("", parts);
    }

    private static string ExtractBaseUnitName(string fullName)
    {
        // Remove common subtitles to get the base name for lookup
        var subtitles = new[]
        {
            " Dark Lord of the Sith",
            " Hero of the Rebellion",
            " Fearless and Inventive",
            " Unorthodox General",
            " Walking Carpet",
            " Stardust",
            " Capable Intelligence Agent",
            " Human Cyborg Relations",
            " Hero of a Thousand Devices",
            " Ruler of the Galactic Empire",
            " Master Tactician",
            " Architect of Terror",
            " Imperial High Command",
            " Inferno Squad Leader",
            " Infamous Bounty Hunter",
            " Trandoshan Terror",
            " Hunter of the Rebellion",
            " Civilized Warrior",
            " The Chosen One",
            " Honorable Soldier",
            " Spirited Senator",
            " Grand Master of the Jedi Order",
            " Darth Tyranus",
            " Sinister Cyborg",
            " A Rival",
            " Hero of Bright Tree",
            " Bringing Order to the Galaxy",
            " Defender of Democracy",
        };

        foreach (var subtitle in subtitles)
        {
            if (fullName.EndsWith(subtitle, StringComparison.OrdinalIgnoreCase))
            {
                return fullName[..^subtitle.Length];
            }
        }

        return fullName;
    }
}

// JSON models for LegionHQ2 format
internal class LegionHqJsonModel
{
    [JsonPropertyName("author")]
    public string Author { get; set; } = "Legion HQ";

    [JsonPropertyName("listname")]
    public string? ListName { get; set; }

    [JsonPropertyName("points")]
    public int Points { get; set; }

    [JsonPropertyName("numActivations")]
    public int NumActivations { get; set; }

    [JsonPropertyName("armyFaction")]
    public string ArmyFaction { get; set; } = string.Empty;

    [JsonPropertyName("commandCards")]
    public List<string> CommandCards { get; set; } = [];

    [JsonPropertyName("contingencies")]
    public List<string> Contingencies { get; set; } = [];

    [JsonPropertyName("units")]
    public List<LegionHqUnitModel> Units { get; set; } = [];

    [JsonPropertyName("battlefieldDeck")]
    public LegionHqBattlefieldModel BattlefieldDeck { get; set; } = new();

    [JsonPropertyName("listlink")]
    public string? ListLink { get; set; }
}

internal class LegionHqUnitModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("upgrades")]
    public List<string> Upgrades { get; set; } = [];
}

internal class LegionHqBattlefieldModel
{
    [JsonPropertyName("scenario")]
    public string Scenario { get; set; } = "standard";

    [JsonPropertyName("conditions")]
    public List<string> Conditions { get; set; } = [];

    [JsonPropertyName("deployment")]
    public List<string> Deployment { get; set; } = [];

    [JsonPropertyName("objective")]
    public List<string> Objective { get; set; } = [];
}
