using LeagueTools.Models;

namespace LeagueTools.Services;

public interface IMatchingGenerator
{
    MonthlyMatchings Generate(League league, int podSize, MatchHistory history, string month, int? seed = null, List<List<string>>? fixedPods = null);
}
