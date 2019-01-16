using System.Collections.Generic;
using System.Linq;
using SailScores.ImportExport.Sailwave.Elements;
using SailScores.ImportExport.Sailwave.Elements.File;

namespace SailScores.ImportExport.Sailwave.Parsers
{
    public class RaceParser : Parser<Race>
    {

        // rows should come in filtered for a single race.
        public Race LoadType(IEnumerable<FileRow> rows,
            IList<Competitor> competitors)
        {
            var race = base.LoadType(rows);
            race.Id = rows
                .FirstOrDefault(c => c.RaceId.HasValue)
                ?.RaceId ?? 0;
            var raceResults = ResultSetParser.GetResults(
                rows.Where(
                    r => r.RowType == RowType.RaceResult
                         && r.RaceId == race.Id),
                competitors);
            race.Results = raceResults;
            return race;
        }
    }
}
