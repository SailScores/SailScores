using System.Collections.Generic;
using System.Linq;
using Sailscores.ImportExport.Sailwave.Elements;

namespace Sailscores.ImportExport.Sailwave.Writers
{
    public class RaceResultSetWriter : GenericSetWriter<RaceResult>
    {
        protected override IEnumerable<RaceResult> GetIndividualItems(Series series)
        {
            return series.Races.SelectMany(r => r.Results);
        }

        protected override int? GetRaceId(RaceResult thing)
        {
            return thing.RaceId;
        }

        protected override int? GetCompetitorOrScoreId(RaceResult thing)
        {
            return thing.CompetitorId;
        }
    }
}
