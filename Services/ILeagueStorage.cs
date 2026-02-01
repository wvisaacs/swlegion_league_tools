using LeagueTools.Models;

namespace LeagueTools.Services;

public interface ILeagueStorage
{
    League? LoadLeague(string eventId);
    void SaveLeague(League league);
    MonthlyMatchings? LoadMatchings(string eventId, string month);
    void SaveMatchings(MonthlyMatchings matchings);
    string GetDataDirectory();
}
