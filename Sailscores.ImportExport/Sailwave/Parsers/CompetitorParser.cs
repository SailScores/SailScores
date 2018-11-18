using System.Collections.Generic;
using System.Linq;
using Sailscores.ImportExport.Sailwave.Elements;
using Sailscores.ImportExport.Sailwave.Elements.File;

namespace Sailscores.ImportExport.Sailwave.Parsers
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
