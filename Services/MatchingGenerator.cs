using LeagueTools.Models;

namespace LeagueTools.Services;

public class MatchingGenerator : IMatchingGenerator
{
    private const int HistoryPenalty = 1000;
    private const int MaxRandomFactor = 10;

    public MonthlyMatchings Generate(
        League league, 
        int podSize, 
        MatchHistory history, 
        string month, 
        List<List<string>>? fixedPods = null,
        int? seed = null)
    {
        if (podSize < 2)
            throw new ArgumentException("Pod size must be at least 2", nameof(podSize));

        if (league.Players.Count < 2)
            throw new ArgumentException("League must have at least 2 players");

        var random = seed.HasValue ? new Random(seed.Value) : new Random();
        fixedPods ??= new List<List<string>>();

        // Validate fixed pods
        var usedPlayerIds = ValidateFixedPods(fixedPods, league);

        // Create fixed pods first
        var pods = new List<Pod>();
        foreach (var fixedPod in fixedPods)
        {
            pods.Add(new Pod
            {
                PlayerIds = fixedPod.ToList(),
                IsOverflow = fixedPod.Count > podSize,
                IsFixed = true
            });
        }

        // Get remaining players (not in fixed pods)
        var remainingPlayers = league.Players
            .Where(p => !usedPlayerIds.Contains(p.Id))
            .ToList();

        // Distribute remaining players using history-aware, standings-based assignment
        if (remainingPlayers.Count > 0)
        {
            // Shuffle first so ties in record get random ordering (preserves seed determinism)
            Shuffle(remainingPlayers, random);
            var additionalPods = DistributeIntoPodsAware(remainingPlayers, podSize, history);
            pods.AddRange(additionalPods);
        }

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

    /// <summary>
    /// Validates fixed pods and returns the set of player IDs used in them.
    /// </summary>
    private HashSet<string> ValidateFixedPods(List<List<string>> fixedPods, League league)
    {
        var allPlayerIds = league.Players.Select(p => p.Id).ToHashSet();
        var usedIds = new HashSet<string>();

        foreach (var pod in fixedPods)
        {
            if (pod.Count < 2)
                throw new ArgumentException($"Fixed pod must have at least 2 players, got {pod.Count}");

            foreach (var id in pod)
            {
                if (!allPlayerIds.Contains(id))
                    throw new ArgumentException($"Player ID '{id}' not found in league");
                if (!usedIds.Add(id))
                    throw new ArgumentException($"Player ID '{id}' appears in multiple fixed pods");
            }
        }

        return usedIds;
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
    /// Computes pod sizes for a given player count and target size.
    /// Returns an array of sizes, where sizes > targetSize are overflow pods.
    /// Example: 14 players, pod 3 → [3, 3, 4, 4]
    /// </summary>
    private static int[] ComputePodSizes(int playerCount, int targetSize)
    {
        if (playerCount <= targetSize + 1)
            return new[] { playerCount };

        var fullPods = playerCount / targetSize;
        var remainder = playerCount % targetSize;
        var overflowCount = remainder;
        var standardCount = fullPods - remainder;

        if (standardCount < 0)
        {
            overflowCount = fullPods;
            standardCount = 0;
        }

        var sizes = new List<int>(standardCount + overflowCount);
        for (int i = 0; i < standardCount; i++) sizes.Add(targetSize);
        for (int i = 0; i < overflowCount; i++) sizes.Add(targetSize + 1);

        // Handle any leftover players (edge case from integer math)
        var assigned = sizes.Sum();
        if (assigned < playerCount && sizes.Count > 0)
            sizes[^1] += playerCount - assigned;
        else if (sizes.Count == 0)
            sizes.Add(playerCount);

        return sizes.ToArray();
    }

    /// <summary>
    /// Distributes players into pods using standings-based seeding and history-aware assignment.
    /// Players are sorted by record (wins desc, losses asc) so same-record players end up together.
    /// Within the same record, the pre-shuffle order is preserved (providing randomness).
    /// Each player is greedily assigned to the pod that minimizes repeat pairings from history.
    /// </summary>
    private static List<Pod> DistributeIntoPodsAware(List<Player> players, int targetSize, MatchHistory history)
    {
        var podSizes = ComputePodSizes(players.Count, targetSize);
        var podCount = podSizes.Length;

        // Sort by record (descending wins, ascending losses); shuffle order is stable tiebreaker
        var sorted = players
            .OrderByDescending(p => p.Wins ?? 0)
            .ThenBy(p => p.Losses ?? 0)
            .ToList();

        // Initialize pod buckets
        var buckets = Enumerable.Range(0, podCount).Select(_ => new List<string>()).ToArray();

        // Greedy assignment: for each player in standings order, find the pod that
        // minimizes the number of historical opponents already assigned to it.
        // Tie-break by current bucket size (prefer evenly filling pods).
        foreach (var player in sorted)
        {
            var bestPod = -1;
            var bestScore = int.MaxValue;

            for (int i = 0; i < podCount; i++)
            {
                if (buckets[i].Count >= podSizes[i]) continue;

                var conflicts = buckets[i].Count(id => history.HavePlayed(player.Id, id));
                // Heavy penalty per conflict; secondary: prefer less-full pods
                var score = conflicts * HistoryPenalty + buckets[i].Count;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestPod = i;
                }
            }

            buckets[bestPod >= 0 ? bestPod : 0].Add(player.Id);
        }

        return buckets.Select((bucket, i) => new Pod
        {
            PlayerIds = bucket,
            IsOverflow = podSizes[i] > targetSize
        }).ToList();
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

