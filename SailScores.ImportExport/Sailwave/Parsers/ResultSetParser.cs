using System.Collections.Generic;
using System.Linq;
using SailScores.ImportExport.Sailwave.Elements;
using SailScores.ImportExport.Sailwave.Elements.File;

namespace SailScores.ImportExport.Sailwave.Parsers
{
    public class ResultSetParser
    {

        // Should only be getting the rows for a single race.
        public static List<RaceResult> GetResults(IEnumerable<FileRow> rows,
            IList<Competitor> competitors)
        {
            var returnList = new List<RaceResult>();
            List<int?> raceIds = rows
                .Where(r => r.RowType == RowType.RaceResult
                && r.RaceId.HasValue)
                .Select(c => c.RaceId)
                .Distinct()
                .ToList();

            List<int?> competitorIds = rows
                .Where(r => r.RowType == RowType.RaceResult
                            && r.CompetitorOrScoringSystemId.HasValue)
                .Select(c => c.CompetitorOrScoringSystemId)
                .Distinct()
                .ToList();

            var parser = new ResultParser();
            foreach (var raceId in raceIds) // really expect there to only be one.
            {
                foreach (var competitorId in competitorIds)
                {
                    var competitor = competitors.FirstOrDefault(c => c.Id == competitorId);
                    var result = parser.LoadType(
                        rows.Where(r => r.RowType == RowType.RaceResult
                                        && r.RaceId == raceId),
                        competitor);
                    if (result != null)
                    {
                        returnList.Add(result);
                    }
                }
            }

            return returnList;
        }
    }
}
