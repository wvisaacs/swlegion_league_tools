using System.Text.RegularExpressions;
using LeagueTools.Models.ListParser;

namespace LeagueTools.Services.ListParser;

/// <summary>
/// Parser for Tabletop Admiral text format lists.
/// </summary>
public partial class TabletopAdmiralParser : IListParser
{
    private readonly CardDatabase _cardDatabase;

    public TabletopAdmiralParser(CardDatabase cardDatabase)
    {
        _cardDatabase = cardDatabase;
    }

    public ListFormat Format => ListFormat.TabletopAdmiral;

    /// <summary>
    /// Detects if text is in Tabletop Admiral format.
    /// TTA format has upgrades prefixed with "--" and no unit type headers.
    /// </summary>
    public bool CanParse(string text)
    {
        // Tabletop Admiral format indicators:
        // - Has upgrade lines starting with "--"
        // - Has "X Activations" on its own line
        // - Units don't have " - " prefix
        // - No "Commanders:", "Corps:" headers
        return text.Contains("--") && 
               Regex.IsMatch(text, @"^\d+\s+Activations?$", RegexOptions.Multiline | RegexOptions.IgnoreCase) &&
               !text.Contains("Commanders:") &&
               !text.Contains("Corps:");
    }

    public ParsedList Parse(string text)
    {
        var result = new ParsedList
        {
            SourceFormat = ListFormat.TabletopAdmiral,
            Author = "LeagueTools"
        };

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                       .Select(l => l.Trim())
                       .ToList();

        // Parse header
        ParseHeader(lines, result);

        // Parse units and upgrades
        ParseUnitsAndUpgrades(lines, result);

        // Parse command cards
        ParseCommandCards(lines, result);

        // Parse battlefield deck
        ParseBattlefieldDeck(lines, result);

        // Detect faction from units
        DetectFaction(result);

        return result;
    }

    private void ParseHeader(List<string> lines, ParsedList result)
    {
        // Pattern: "1000/1000" on first line
        var pointsMatch = PointsRegex().Match(lines.FirstOrDefault() ?? "");
        if (pointsMatch.Success)
        {
            result.Points = int.Parse(pointsMatch.Groups[1].Value);
        }

        // Pattern: "12 Activations" on second line
        foreach (var line in lines.Take(3))
        {
            var activationsMatch = ActivationsRegex().Match(line);
            if (activationsMatch.Success)
            {
                result.NumActivations = int.Parse(activationsMatch.Groups[1].Value);
                break;
            }
        }
    }

    private void ParseUnitsAndUpgrades(List<string> lines, ParsedList result)
    {
        ParsedUnit? currentUnit = null;

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            // Skip header lines
            if (PointsRegex().IsMatch(line) || ActivationsRegex().IsMatch(line))
                continue;

            // Skip command cards and battlefield deck lines
            if (IsCommandCardLine(line) || IsBattlefieldLine(line))
                continue;

            // Check if this is an upgrade line (starts with --)
            if (line.StartsWith("--"))
            {
                if (currentUnit != null)
                {
                    var upgrades = ParseUpgradeLine(line);
                    currentUnit.Upgrades.AddRange(upgrades);
                }
                continue;
            }

            // Try to parse as a unit line - may return multiple units
            var units = ParseUnitLine(line);
            if (units.Count > 0)
            {
                result.Units.AddRange(units);
                currentUnit = units[^1]; // Last unit gets upgrades
            }
        }
    }

    private List<ParsedUnit> ParseUnitLine(string line)
    {
        var result = new List<ParsedUnit>();
        
        // Check for multiplied units: "2x Rebel Troopers 40 x 2 = 80"
        var multiMatch = MultipliedUnitRegex().Match(line);
        if (multiMatch.Success)
        {
            var count = int.Parse(multiMatch.Groups[1].Value);
            var unitName = multiMatch.Groups[2].Value.Trim();

            for (int i = 0; i < count; i++)
            {
                result.Add(new ParsedUnit
                {
                    Name = ResolveFullUnitName(unitName),
                    Upgrades = []
                });
            }
            return result;
        }

        // Pattern: "Han Solo 100 + 13 = 113" or "Wicket 70 + 3 = 73"
        var unitWithUpgradesMatch = UnitWithCostRegex().Match(line);
        if (unitWithUpgradesMatch.Success)
        {
            var unitName = unitWithUpgradesMatch.Groups[1].Value.Trim();
            result.Add(new ParsedUnit
            {
                Name = ResolveFullUnitName(unitName),
                Upgrades = []
            });
            return result;
        }

        // Pattern: "2x Rebel Troopers 40 x 2 = 80" (unit without upgrades) - already handled above
        var simpleMultiMatch = SimpleMultipliedUnitRegex().Match(line);
        if (simpleMultiMatch.Success)
        {
            var count = int.Parse(simpleMultiMatch.Groups[1].Value);
            var unitName = simpleMultiMatch.Groups[2].Value.Trim();
            for (int i = 0; i < count; i++)
            {
                result.Add(new ParsedUnit
                {
                    Name = ResolveFullUnitName(unitName),
                    Upgrades = []
                });
            }
            return result;
        }

        return result;
    }

    private List<string> ParseUpgradeLine(string line)
    {
        // Remove leading "--"
        var upgradesText = line.TrimStart('-').Trim();

        var upgrades = new List<string>();

        // Pattern: "Upgrade Name (cost), Another Upgrade (cost)"
        var parts = upgradesText.Split(',');
        foreach (var part in parts)
        {
            var match = UpgradeRegex().Match(part.Trim());
            if (match.Success)
            {
                var upgradeName = match.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(upgradeName))
                {
                    upgrades.Add(ResolveFullUpgradeName(upgradeName));
                }
            }
        }

        return upgrades;
    }

    private void ParseCommandCards(List<string> lines, ParsedList result)
    {
        // Find command cards line - contains pip symbols or asterisks
        foreach (var line in lines)
        {
            if (!IsCommandCardLine(line))
                continue;

            // Split by comma and extract command names (removing pip symbols and asterisks)
            var commands = line.Split(',')
                .Select(c => 
                {
                    var trimmed = c.Trim();
                    // Remove trailing pip symbols (•) or asterisks (*)
                    while (trimmed.Length > 0 && (trimmed[^1] == '•' || trimmed[^1] == '*'))
                    {
                        trimmed = trimmed[..^1].Trim();
                    }
                    // Also remove leading ones
                    while (trimmed.Length > 0 && (trimmed[0] == '•' || trimmed[0] == '*' || !char.IsLetter(trimmed[0])))
                    {
                        trimmed = trimmed[1..].TrimStart();
                    }
                    return trimmed;
                })
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList();

            result.CommandCards = commands;
            break;
        }
    }

    private void ParseBattlefieldDeck(List<string> lines, ParsedList result)
    {
        // Look for battlefield deck lines - typically the last 3 lines
        // In order: Objectives, Deployments, Conditions (based on TTA format)
        var battlefieldLines = lines
            .Where(IsBattlefieldLine)
            .ToList();

        // First, try to identify by known card names
        var usedLines = new HashSet<int>();
        
        for (int i = 0; i < battlefieldLines.Count; i++)
        {
            var cards = battlefieldLines[i].Split(',')
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList();

            if (IsConditionLine(cards) && result.BattlefieldDeck.Conditions.Count == 0)
            {
                result.BattlefieldDeck.Conditions = cards;
                usedLines.Add(i);
            }
            else if (IsDeploymentLine(cards) && result.BattlefieldDeck.Deployment.Count == 0)
            {
                result.BattlefieldDeck.Deployment = cards;
                usedLines.Add(i);
            }
        }

        // Assign remaining lines by position (first remaining = objectives)
        for (int i = 0; i < battlefieldLines.Count; i++)
        {
            if (usedLines.Contains(i)) continue;
            
            var cards = battlefieldLines[i].Split(',')
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList();

            if (result.BattlefieldDeck.Objective.Count == 0)
            {
                result.BattlefieldDeck.Objective = cards;
            }
            else if (result.BattlefieldDeck.Deployment.Count == 0)
            {
                result.BattlefieldDeck.Deployment = cards;
            }
            else if (result.BattlefieldDeck.Conditions.Count == 0)
            {
                result.BattlefieldDeck.Conditions = cards;
            }
        }
    }

    private static bool IsCommandCardLine(string line)
    {
        // Command cards have pip symbols (•, ••, •••, ••••) or asterisks (*, **, ***, ****)
        return line.Contains('•') || 
               (line.Contains('*') && !line.Contains("--") && line.Contains(','));
    }

    private static bool IsBattlefieldLine(string line)
    {
        // Battlefield lines don't have pip symbols, asterisks, costs, or upgrade patterns
        return !line.Contains('•') &&
               !line.Contains('*') &&  // Exclude command card lines with asterisks
               !line.Contains("--") &&
               !Regex.IsMatch(line, @"\d+\s*[+x=]") &&
               !Regex.IsMatch(line, @"^\d+/\d+") &&
               !Regex.IsMatch(line, @"^\d+\s+Activations?", RegexOptions.IgnoreCase) &&
               line.Contains(',') && // Battlefield lines are comma-separated
               line.Split(',').All(p => !p.Contains('(') && !p.Contains(')')); // No costs
    }

    private static readonly HashSet<string> KnownObjectives = new(StringComparer.OrdinalIgnoreCase)
    {
        "Breakthrough", "Outflank", "Shifting Priorities", "Sweep and Clear",
        "Recon Mission", "Supply Run", "Key Positions", "Recover the Supplies",
        "Intercept the Transmissions", "Bring Them to Heel", "Bombing Run",
        "Hostage Exchange", "Payload", "Sabotage the Moisture Vaporators"
    };

    private static readonly HashSet<string> KnownDeployments = new(StringComparer.OrdinalIgnoreCase)
    {
        "Battle Lines", "Major Offensive", "Advanced Positions", "Hemmed In",
        "The Long March", "Disarray", "Roll Out", "Danger Close"
    };

    private static readonly HashSet<string> KnownConditions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Clear Conditions", "Hostile Environment", "Limited Visibility", "Minefield",
        "Rapid Reinforcements", "War Weary", "Fortified Positions", "Supply Drop",
        "Cunning Deployment", "No Time to Lose", "Advanced Intel"
    };

    private static bool IsObjectiveLine(List<string> cards)
    {
        return cards.Any(c => KnownObjectives.Contains(c.Trim()));
    }

    private static bool IsDeploymentLine(List<string> cards)
    {
        return cards.Any(c => KnownDeployments.Contains(c.Trim()));
    }

    private static bool IsConditionLine(List<string> cards)
    {
        return cards.Any(c => KnownConditions.Contains(c.Trim()));
    }

    private void DetectFaction(ParsedList result)
    {
        foreach (var unit in result.Units)
        {
            var faction = _cardDatabase.DetectFactionFromUnit(unit.Name);
            if (!string.IsNullOrEmpty(faction))
            {
                result.ArmyFaction = NormalizeFaction(faction);
                return;
            }
        }
    }

    private string ResolveFullUnitName(string shortName)
    {
        var card = _cardDatabase.GetByName(shortName);
        return card?.FullName ?? shortName;
    }

    private string ResolveFullUpgradeName(string shortName)
    {
        var card = _cardDatabase.GetByName(shortName);
        return card?.CardName ?? shortName;
    }

    private static string NormalizeFaction(string faction)
    {
        return faction.ToLowerInvariant() switch
        {
            "rebels" => "rebel",
            "empire" => "empire",
            "republic" => "republic",
            "separatists" => "separatist",
            "separatist" => "separatist",
            _ => faction.ToLowerInvariant()
        };
    }

    // Regex patterns
    [GeneratedRegex(@"^(\d+)/(\d+)$")]
    private static partial Regex PointsRegex();

    [GeneratedRegex(@"^(\d+)\s+Activations?$", RegexOptions.IgnoreCase)]
    private static partial Regex ActivationsRegex();

    [GeneratedRegex(@"^(\d+)x\s+(.+?)\s+\d+\s*[x×]\s*\d+\s*=\s*\d+$", RegexOptions.IgnoreCase)]
    private static partial Regex MultipliedUnitRegex();

    [GeneratedRegex(@"^(\d+)x\s+(.+?)\s+\d+\s*=\s*\d+$", RegexOptions.IgnoreCase)]
    private static partial Regex SimpleMultipliedUnitRegex();

    [GeneratedRegex(@"^(.+?)\s+\d+\s*\+\s*\d+\s*=\s*\d+$")]
    private static partial Regex UnitWithCostRegex();

    [GeneratedRegex(@"^([^(]+)(?:\s*\(\d+\))?$")]
    private static partial Regex UpgradeRegex();

    [GeneratedRegex(@"[•]+")]
    private static partial Regex CommandPipRegex();
}
