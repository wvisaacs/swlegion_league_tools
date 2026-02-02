using LeagueTools.Models;

namespace LeagueTools.Services;

public interface IMatchingGenerator
{
    MonthlyMatchings Generate(
        League league, 
        int podSize, 
        MatchHistory history, 
        string month, 
        List<List<string>>? fixedPods = null,
        int? seed = null);
}
