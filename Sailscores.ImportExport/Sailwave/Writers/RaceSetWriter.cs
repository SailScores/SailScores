using System.Collections.Generic;
using Sailscores.ImportExport.Sailwave.Elements;

namespace Sailscores.ImportExport.Sailwave.Writers
{
    public class RaceSetWriter : GenericSetWriter<Race>
    {
        protected override IEnumerable<Race> GetIndividualItems(Series series)
        {
            return series.Races;
        }

        protected override int? GetRaceId(Race thing)
        {
            return thing.Id;
        }

        protected override int? GetCompetitorOrScoreId(Race thing)
        {
            return null;
        }
    }
}
