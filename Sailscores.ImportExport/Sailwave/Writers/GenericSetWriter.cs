using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SailScores.ImportExport.Sailwave.Attributes;
using SailScores.ImportExport.Sailwave.Elements;
using SailScores.ImportExport.Sailwave.Elements.File;

namespace SailScores.ImportExport.Sailwave.Writers
{
    public abstract class GenericSetWriter<T> : GenericWriter<T>
        where T : new() 
    {
        public virtual async Task<IEnumerable<FileRow>> GetRowsForSet(Series series)
        {
            var returnList = new List<FileRow>();
            foreach (var thing in GetIndividualItems(series))
            {
                var rows = await GetRows(thing);
                var raceId = GetRaceId(thing);
                var compId = GetCompetitorOrScoreId(thing);
                foreach (var row in rows)
                {
                    row.RaceId = raceId;
                    row.CompetitorOrScoringSystemId = compId;
                }
                returnList.AddRange(rows);
            }

            return returnList;
        }

        protected abstract IEnumerable<T> GetIndividualItems(Series series);
        protected abstract int? GetRaceId(T thing);
        protected abstract int? GetCompetitorOrScoreId(T thing);
        

    }
}
