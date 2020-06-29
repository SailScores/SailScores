using System.Collections.Generic;
using SailScores.ImportExport.Sailwave.Elements;

namespace SailScores.ImportExport.Sailwave.Writers
{
    public class CompetitorSetWriter : GenericSetWriter<Competitor>
    {
        protected override IEnumerable<Competitor> GetIndividualItems(Series series)
        {
            return series.Competitors;
        }

        protected override int? GetRaceId(Competitor thing)
        {
            return null;
        }

        protected override int? GetCompetitorOrScoreId(Competitor thing)
        {
            return thing.Id;
        }
    }
}
