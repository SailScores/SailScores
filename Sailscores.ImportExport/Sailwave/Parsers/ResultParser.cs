using System.Collections.Generic;
using System.Linq;
using SailScores.ImportExport.Sailwave.Elements;
using SailScores.ImportExport.Sailwave.Elements.File;

namespace SailScores.ImportExport.Sailwave.Parsers
{
    public class ResultParser : Parser<RaceResult>
    {
        public RaceResult LoadType(
            IEnumerable<FileRow> rows,
            Competitor competitor)
        {
            var result = base.LoadType(rows.Where(r => r.CompetitorOrScoringSystemId == competitor.Id));
            result.RaceId = rows
                .FirstOrDefault(c => c.RaceId.HasValue)
                ?.RaceId ?? 0;
            result.CompetitorId = competitor.Id;
            return result;
        }
    }
}
