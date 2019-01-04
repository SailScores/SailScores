using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SailScores.ImportExport.Sailwave.Elements;
using SailScores.ImportExport.Sailwave.Elements.File;

namespace SailScores.ImportExport.Sailwave.Writers
{
    public class ScoringSystemSetWriter : GenericSetWriter<ScoringSystem>
    {
        public ScoringSystemSetWriter() :base()
        {
            BoolsAsYesNo = true;
        }

        public override async Task<IEnumerable<FileRow>> GetRows(ScoringSystem thing)
        {
            var rows = (await base.GetRows(thing)).ToList();
            foreach (var code in thing.Codes)
            {
                rows.Add(ScoreCodeWriter.GetRow(code));
            }
            return rows;
        }

        public override async Task<IEnumerable<FileRow>> GetRowsForSet(Series series)
        {
            var rows = await base.GetRowsForSet(series);
            foreach (var row in rows)
            {
                if (row.Name == RowNames.ScoreCode)
                {
                    row.CompetitorOrScoringSystemId = null;
                }
            }
            return rows;
        }

        protected override IEnumerable<ScoringSystem> GetIndividualItems(Series series)
        {
            return series.ScoringSystems;
        }

        protected override int? GetRaceId(ScoringSystem thing)
        {
            return null;
        }

        protected override int? GetCompetitorOrScoreId(ScoringSystem thing)
        {
            return thing.Id;
        }
    }
}
