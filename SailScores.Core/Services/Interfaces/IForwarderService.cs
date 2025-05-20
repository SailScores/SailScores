using System.Threading.Tasks;
using SailScores.Core.Model;
using SailScores.Core.Model.Forwarder;


namespace SailScores.Core.Services;

public interface IForwarderService
{
    Task<SeriesForwarderResult> GetSeriesForwarding(string clubInitials, string seasonUrlName, string seriesUrlName);
    Task<RegattaForwarderResult> GetRegattaForwarding(string clubInitials, string seasonUrlName, string regattaUrlName);
    Task<CompetitorForwarderResult> GetCompetitorForwarding(string clubInitials, string sailNumber);
    Task CreateCompetitorForwarder(Competitor newCompetitor, Database.Entities.Competitor oldCompetitor);
    Task CreateRegattaForwarder(Regatta newRegatta, Database.Entities.Regatta oldRegatta);
    Task CreateSeriesForwarder(Series newSeries, Database.Entities.Series oldSeries);
}