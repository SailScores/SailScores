using System.Collections.Generic;
using System.Linq;
using Sailscores.ImportExport.Sailwave.Elements;
using Sailscores.ImportExport.Sailwave.Elements.File;

namespace Sailscores.ImportExport.Sailwave.Parsers
{
    public class RaceSetParser
    {
        public static List<Race> GetRaces(IEnumerable<FileRow> rows,
            IList<Competitor> competitors)
        {
            var returnList = new List<Race>();
            List<int?> raceIds = rows
                .Where(r => r.RowType == RowType.Race
                    && r.RaceId.HasValue)
                .Select(c => c.RaceId)
                .Distinct()
                .ToList();
            var parser = new RaceParser();
            foreach (var raceId in raceIds)
            {
                var race = parser.LoadType(
                    rows.Where(r => (r.RowType == RowType.Race
                            || r.RowType == RowType.RaceResult)
                            && r.RaceId == raceId),
                    competitors);
                returnList.Add(race);
            }

            return returnList;
        }
    }
}
