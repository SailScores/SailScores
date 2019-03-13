using System;
using System.Collections.Generic;
using System.Text;

//This namespace is likely to get moved to Api project
namespace SailScores.Core.FlatModel
{
    public class FlatResults
    {
        public Guid SeriesId { get; set; }

        public IEnumerable<FlatCompetitor> Competitors { get; set; }
        public IEnumerable<FlatRace> Races { get; set; }
        public IEnumerable<FlatCalculatedScore> CalculatedScores { get; set; }

    }
}
