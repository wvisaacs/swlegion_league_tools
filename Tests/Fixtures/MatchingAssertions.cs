using LeagueTools.Models;

namespace LeagueTools.Tests.Fixtures;

public static class MatchingAssertions
{
    /// <summary>
    /// Asserts that every player appears in exactly one pod.
    /// </summary>
    public static void AssertAllPlayersInExactlyOnePod(MonthlyMatchings matchings, League league)
    {
        var allPlayerIds = league.Players.Select(p => p.Id).ToHashSet();
        var assignedPlayerIds = new HashSet<string>();

        foreach (var pod in matchings.Pods)
        {
            foreach (var playerId in pod.PlayerIds)
            {
                if (!assignedPlayerIds.Add(playerId))
                {
                    throw new Exception($"Player {playerId} appears in multiple pods");
                }
            }
        }

        var missingPlayers = allPlayerIds.Except(assignedPlayerIds).ToList();
        if (missingPlayers.Any())
        {
            throw new Exception($"Players not assigned to any pod: {string.Join(", ", missingPlayers)}");
        }

        var extraPlayers = assignedPlayerIds.Except(allPlayerIds).ToList();
        if (extraPlayers.Any())
        {
            throw new Exception($"Unknown players in pods: {string.Join(", ", extraPlayers)}");
        }
    }

    /// <summary>
    /// Asserts that no player is matched against themselves.
    /// </summary>
    public static void AssertNoSelfMatches(MonthlyMatchings matchings)
    {
        foreach (var pod in matchings.Pods)
        {
            foreach (var match in pod.Matches)
            {
                if (match.Player1Id == match.Player2Id)
                {
                    throw new Exception($"Self-match found: {match.Player1Id} vs {match.Player2Id}");
                }
            }
        }
    }

    /// <summary>
    /// Asserts that no match appears twice within a pod.
    /// </summary>
    public static void AssertNoDuplicateMatches(MonthlyMatchings matchings)
    {
        foreach (var pod in matchings.Pods)
        {
            var seenMatches = new HashSet<string>();
            foreach (var match in pod.Matches)
            {
                // Normalize match key (smaller ID first)
                var key = string.Compare(match.Player1Id, match.Player2Id) < 0
                    ? $"{match.Player1Id}-{match.Player2Id}"
                    : $"{match.Player2Id}-{match.Player1Id}";

                if (!seenMatches.Add(key))
                {
                    throw new Exception($"Duplicate match in pod {pod.PodId}: {match.Player1Id} vs {match.Player2Id}");
                }
            }
        }
    }

    /// <summary>
    /// Asserts that standard pods have the correct round-robin match count.
    /// </summary>
    public static void AssertStandardPodMatchCount(Pod pod)
    {
        if (pod.IsOverflow) return;

        var n = pod.PlayerIds.Count;
        var expectedMatches = n * (n - 1) / 2;
        
        if (pod.Matches.Count != expectedMatches)
        {
            throw new Exception($"Pod {pod.PodId} has {pod.Matches.Count} matches, expected {expectedMatches} for {n} players");
        }
    }

    /// <summary>
    /// Asserts all players in a pod are involved in at least one match.
    /// </summary>
    public static void AssertAllPodPlayersHaveMatches(Pod pod)
    {
        var playersWithMatches = new HashSet<string>();
        foreach (var match in pod.Matches)
        {
            playersWithMatches.Add(match.Player1Id);
            playersWithMatches.Add(match.Player2Id);
        }

        var playersWithoutMatches = pod.PlayerIds.Except(playersWithMatches).ToList();
        if (playersWithoutMatches.Any())
        {
            throw new Exception($"Players in pod {pod.PodId} without matches: {string.Join(", ", playersWithoutMatches)}");
        }
    }

    /// <summary>
    /// Runs all sanity checks on matchings.
    /// </summary>
    public static void AssertValidMatchings(MonthlyMatchings matchings, League league)
    {
        AssertAllPlayersInExactlyOnePod(matchings, league);
        AssertNoSelfMatches(matchings);
        AssertNoDuplicateMatches(matchings);
        
        foreach (var pod in matchings.Pods)
        {
            AssertStandardPodMatchCount(pod);
            AssertAllPodPlayersHaveMatches(pod);
        }
    }
}
