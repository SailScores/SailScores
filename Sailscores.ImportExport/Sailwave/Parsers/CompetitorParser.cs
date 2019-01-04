using System.Collections.Generic;
using System.Linq;
using SailScores.ImportExport.Sailwave.Elements;
using SailScores.ImportExport.Sailwave.Elements.File;

namespace SailScores.ImportExport.Sailwave.Parsers
{
    public class CompetitorParser : Parser<Competitor>
    {
        public override Competitor LoadType(IEnumerable<FileRow> rows)
        {
            var comp = base.LoadType(rows);
            comp.Id = rows
                .FirstOrDefault(c => c.CompetitorOrScoringSystemId.HasValue)
                ?.CompetitorOrScoringSystemId ?? 0;
            return comp;
        }
    }
}
