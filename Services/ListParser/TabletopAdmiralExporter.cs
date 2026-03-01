using System.Text.Json;
using System.Text.Json.Serialization;
using LeagueTools.Models.ListParser;

namespace LeagueTools.Services.ListParser;

/// <summary>
/// Exports lists in Tabletop Admiral-compatible JSON format.
/// Note: URL generation is not supported (requires TTA's card ID database).
/// </summary>
public class TabletopAdmiralExporter : IListExporter
{
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

    public string ExportToJson(ParsedList list, bool indented = true)
    {
        var exportModel = new TabletopAdmiralJsonModel
        {
            ListName = list.ListName,
            Points = list.Points,
            Author = "Tabletop Admiral",
            NumActivations = list.NumActivations,
            ArmyFaction = list.ArmyFaction,
            BattleForce = list.BattleForce,
            CommandCards = list.CommandCards,
            Contingencies = list.Contingencies,
            Units = list.Units.Select(u => new TabletopAdmiralUnitModel
            {
                Name = u.Name,
                Upgrades = u.Upgrades,
                Loadout = u.Loadout
            }).ToList(),
            BattlefieldDeck = new TabletopAdmiralBattlefieldModel
            {
                Scenario = list.BattlefieldDeck.Scenario,
                Conditions = list.BattlefieldDeck.Conditions,
                Deployment = list.BattlefieldDeck.Deployment,
                Objective = list.BattlefieldDeck.Objective
            }
        };

        return JsonSerializer.Serialize(exportModel, indented ? JsonOptions : JsonOptionsCompact);
    }

    /// <summary>
    /// URL generation is not supported for Tabletop Admiral.
    /// See backlog issue: Tabletop Admiral URL Generation.
    /// </summary>
    public string? GenerateUrl(ParsedList list)
    {
        // URL generation requires Tabletop Admiral's card ID database
        // which is not publicly available.
        return null;
    }
}

// JSON models for Tabletop Admiral format
internal class TabletopAdmiralJsonModel
{
    [JsonPropertyName("listname")]
    public string? ListName { get; set; }

    [JsonPropertyName("points")]
    public int Points { get; set; }

    [JsonPropertyName("author")]
    public string Author { get; set; } = "Tabletop Admiral";

    [JsonPropertyName("numActivations")]
    public int NumActivations { get; set; }

    [JsonPropertyName("armyFaction")]
    public string ArmyFaction { get; set; } = string.Empty;

    [JsonPropertyName("battleForce")]
    public string? BattleForce { get; set; }

    [JsonPropertyName("commandCards")]
    public List<string> CommandCards { get; set; } = [];

    [JsonPropertyName("contingencies")]
    public List<string> Contingencies { get; set; } = [];

    [JsonPropertyName("units")]
    public List<TabletopAdmiralUnitModel> Units { get; set; } = [];

    [JsonPropertyName("battlefieldDeck")]
    public TabletopAdmiralBattlefieldModel BattlefieldDeck { get; set; } = new();

    [JsonPropertyName("listlink")]
    public string? ListLink { get; set; }
}

internal class TabletopAdmiralUnitModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("upgrades")]
    public List<string> Upgrades { get; set; } = [];

    [JsonPropertyName("loadout")]
    public List<string> Loadout { get; set; } = [];
}

internal class TabletopAdmiralBattlefieldModel
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
