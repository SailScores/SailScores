using System.Collections.Generic;
using System.Linq;
using SailScores.ImportExport.Sailwave.Elements;
using SailScores.ImportExport.Sailwave.Elements.File;

namespace SailScores.ImportExport.Sailwave.Parsers
{
    public class ScoringSystemParser : Parser<ScoringSystem>
    {

        // rows should come in filtered for a single ScoringSystem.
        public override ScoringSystem LoadType(IEnumerable<FileRow> rows)
        {
            var scoreSystem = base.LoadType(rows);
            scoreSystem.Id = rows
                .FirstOrDefault(c => c.CompetitorOrScoringSystemId.HasValue)
                ?.CompetitorOrScoringSystemId ?? 0;

            return scoreSystem;
        }
    }
}
