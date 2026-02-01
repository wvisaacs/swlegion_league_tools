using LeagueTools.Models;

namespace LeagueTools.Services;

public interface IHistoryService
{
    MatchHistory LoadHistory(string eventId);
    void SaveHistory(string eventId, MatchHistory history);
    void RecordMatches(string eventId, MonthlyMatchings matchings);
}
