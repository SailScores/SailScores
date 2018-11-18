using System;
using System.Collections.Generic;
using System.Linq;
using Sailscores.ImportExport.Sailwave.Elements;
using Sailscores.ImportExport.Sailwave.Elements.File;

namespace Sailscores.ImportExport.Sailwave.Parsers
{
    public class ScoringSystemSetParser
    {
        public static List<ScoringSystem> GetScoringSystems(IEnumerable<FileRow> rows)
        {
            var returnList = new List<ScoringSystem>();
            List<int?> scoringSystemIds = rows
                .Where(r => r.RowType == RowType.ScoringSystem
                    && r.CompetitorOrScoringSystemId.HasValue)
                .Select(c => c.CompetitorOrScoringSystemId)
                .Distinct()
                .ToList();
            var allScoreCodes = GetAllScoreCodes(rows);

            var parser = new ScoringSystemParser();
            foreach (var scoringSystemId in scoringSystemIds)
            {
                var scoreSystem = parser.LoadType(
                    rows.Where(r => (r.RowType == RowType.ScoringSystem)
                                    && r.CompetitorOrScoringSystemId == scoringSystemId));
                scoreSystem.Codes = allScoreCodes.Where(c => c.ScoringSystemId == scoringSystemId).ToList();

                returnList.Add(scoreSystem);
            }

            return returnList;
        }

        private static List<ScoreCode> GetAllScoreCodes(IEnumerable<FileRow> rows)
        {
            var returnCodes = new List<ScoreCode>();
            var codeRows = rows.Where(r => r.RowType == RowType.ScoringSystem
                                           && r.Name.Equals("scrcode", StringComparison.CurrentCultureIgnoreCase));
            foreach (var codeRow in codeRows)
            {
                returnCodes.Add(ScoreCodeParser.GetCode(codeRow));
            }

            return returnCodes;
        }
    }
}
