using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sailscores.ImportExport.Sailwave.Elements;
using Sailscores.ImportExport.Sailwave.Elements.File;
using Sailscores.ImportExport.Sailwave.Writers;

namespace Sailscores.ImportExport.Sailwave.Writers
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
