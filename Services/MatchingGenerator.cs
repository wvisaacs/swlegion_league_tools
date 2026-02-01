using LeagueTools.Models;

namespace LeagueTools.Services;

public class MatchingGenerator : IMatchingGenerator
{
    private const int HistoryPenalty = 1000;
    private const int MaxRandomFactor = 10;

    public MonthlyMatchings Generate(League league, int podSize, MatchHistory history, string month, int? seed = null)
    {
        if (podSize < 2)
            throw new ArgumentException("Pod size must be at least 2", nameof(podSize));

        if (league.Players.Count < 2)
            throw new ArgumentException("League must have at least 2 players");

        var random = seed.HasValue ? new Random(seed.Value) : new Random();
        var players = league.Players.ToList();

        // Shuffle players randomly
        Shuffle(players, random);

        // Distribute players into pods
        var pods = DistributeIntoPods(players, podSize);

        // Generate matches for each pod
        var podId = 1;
        foreach (var pod in pods)
        {
            pod.PodId = podId++;
            GenerateMatchesForPod(pod, league.Players, history, month, random);
        }

        return new MonthlyMatchings
        {
            EventId = league.EventId,
            Month = month,
            TargetPodSize = podSize,
            GeneratedAt = DateTime.UtcNow,
            Pods = pods
        };
    }

    private static void Shuffle<T>(List<T> list, Random random)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// Distributes players into pods, minimizing size variance.
    /// Example: 14 players with pod size 3 → 2 pods of 3 + 2 pods of 4
    /// </summary>
    private List<Pod> DistributeIntoPods(List<Player> players, int targetSize)
    {
        var pods = new List<Pod>();
        var playerCount = players.Count;

        if (playerCount <= targetSize + 1)
        {
            // Everyone in one pod (possibly overflow)
            pods.Add(new Pod
            {
                PlayerIds = players.Select(p => p.Id).ToList(),
                IsOverflow = playerCount > targetSize
            });
            return pods;
        }

        var fullPods = playerCount / targetSize;
        var remainder = playerCount % targetSize;

        // Distribute remainder by making some pods size+1
        // This minimizes variance
        var overflowPodCount = remainder;
        var standardPodCount = fullPods - remainder;

        // Edge case: if we'd have negative standard pods, adjust
        if (standardPodCount < 0)
        {
            overflowPodCount = fullPods;
            standardPodCount = 0;
        }

        var playerIndex = 0;

        // Create standard pods (size = targetSize)
        for (int i = 0; i < standardPodCount; i++)
        {
            var pod = new Pod { IsOverflow = false };
            for (int j = 0; j < targetSize && playerIndex < playerCount; j++)
            {
                pod.PlayerIds.Add(players[playerIndex++].Id);
            }
            pods.Add(pod);
        }

        // Create overflow pods (size = targetSize + 1)
        for (int i = 0; i < overflowPodCount; i++)
        {
            var pod = new Pod { IsOverflow = true };
            var podTargetSize = targetSize + 1;
            for (int j = 0; j < podTargetSize && playerIndex < playerCount; j++)
            {
                pod.PlayerIds.Add(players[playerIndex++].Id);
            }
            pods.Add(pod);
        }

        // Handle any remaining players (shouldn't happen with correct math)
        if (playerIndex < playerCount && pods.Count > 0)
        {
            var lastPod = pods.Last();
            while (playerIndex < playerCount)
            {
                lastPod.PlayerIds.Add(players[playerIndex++].Id);
                lastPod.IsOverflow = true;
            }
        }

        return pods;
    }

    /// <summary>
    /// Generates matches for a pod.
    /// Standard pods: full round-robin (everyone plays everyone)
    /// Overflow pods: weighted matching to give each player (targetSize - 1) games while avoiding history repeats
    /// </summary>
    private void GenerateMatchesForPod(Pod pod, List<Player> allPlayers, MatchHistory history, string month, Random random)
    {
        var playerLookup = allPlayers.ToDictionary(p => p.Id);
        var podPlayerIds = pod.PlayerIds;
        var n = podPlayerIds.Count;

        if (!pod.IsOverflow)
        {
            // Standard pod: full round-robin
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    var p1 = playerLookup[podPlayerIds[i]];
                    var p2 = playerLookup[podPlayerIds[j]];
                    pod.Matches.Add(CreateMatch(p1, p2));
                }
            }
        }
        else
        {
            // Overflow pod: weighted matching
            // Target: each player plays (n - 2) games (same as they would in a standard pod of size n-1)
            var targetGamesPerPlayer = n - 2;
            if (targetGamesPerPlayer < 1) targetGamesPerPlayer = 1;

            var matches = GenerateWeightedMatches(podPlayerIds, playerLookup, history, targetGamesPerPlayer, random);
            pod.Matches.AddRange(matches);
        }
    }

    /// <summary>
    /// Generates weighted matches for overflow pods.
    /// Uses greedy selection with penalties for historical matchups.
    /// </summary>
    private List<Match> GenerateWeightedMatches(
        List<string> playerIds,
        Dictionary<string, Player> playerLookup,
        MatchHistory history,
        int targetGamesPerPlayer,
        Random random)
    {
        var n = playerIds.Count;
        var gameCounts = playerIds.ToDictionary(id => id, _ => 0);
        var selectedMatches = new List<Match>();

        // Generate all possible pairs with weights
        var pairs = new List<(string P1, string P2, int Weight)>();
        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                var p1 = playerIds[i];
                var p2 = playerIds[j];
                var weight = CalculatePairWeight(p1, p2, history, random);
                pairs.Add((p1, p2, weight));
            }
        }

        // Sort by weight (ascending - lower is better)
        pairs = pairs.OrderBy(p => p.Weight).ToList();

        // Greedy selection
        foreach (var (p1, p2, _) in pairs)
        {
            // Check if both players still need games
            if (gameCounts[p1] < targetGamesPerPlayer && gameCounts[p2] < targetGamesPerPlayer)
            {
                selectedMatches.Add(CreateMatch(playerLookup[p1], playerLookup[p2]));
                gameCounts[p1]++;
                gameCounts[p2]++;
            }
        }

        // If some players don't have enough games, allow repeat selections with warning
        var underserved = gameCounts.Where(kv => kv.Value < targetGamesPerPlayer).ToList();
        if (underserved.Any())
        {
            Console.WriteLine($"Warning: Some players in overflow pod have fewer than {targetGamesPerPlayer} games due to constraints.");
            
            // Try to add more matches for underserved players
            foreach (var (p1, p2, _) in pairs)
            {
                if (gameCounts[p1] < targetGamesPerPlayer || gameCounts[p2] < targetGamesPerPlayer)
                {
                    // Check if this exact match already exists
                    if (!selectedMatches.Any(m => m.Involves(p1) && m.Involves(p2)))
                    {
                        selectedMatches.Add(CreateMatch(playerLookup[p1], playerLookup[p2]));
                        gameCounts[p1]++;
                        gameCounts[p2]++;
                    }
                }

                if (gameCounts.All(kv => kv.Value >= targetGamesPerPlayer))
                    break;
            }
        }

        return selectedMatches;
    }

    private int CalculatePairWeight(string p1, string p2, MatchHistory history, Random random)
    {
        var weight = 0;

        // Heavy penalty for historical matchups
        var timesPlayed = history.TimesPlayed(p1, p2);
        weight += timesPlayed * HistoryPenalty;

        // Small random factor for variety
        weight += random.Next(MaxRandomFactor);

        return weight;
    }

    private static Match CreateMatch(Player p1, Player p2) => new()
    {
        Player1Id = p1.Id,
        Player2Id = p2.Id,
        Player1Name = p1.DisplayName,
        Player2Name = p2.DisplayName
    };
}
