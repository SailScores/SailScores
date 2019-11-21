﻿using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Web.Models.SailScores
{
    public class MergeCompetitorViewModel
    {
        public IList<Competitor> TargetCompetitorOptions { get; set; }
        public IList<Competitor> SourceCompetitorOptions { get; set; }
        public Competitor TargetCompetitor { get; set; }
        public Guid? TargetCompetitorId { get; set; }
        public Competitor SourceCompetitor { get; set; }
        public Guid? SourceCompetitorId { get; set; }

        public int? TargetNumberOfRaces { get; set; }

        public IList<Season> TargetSeasons { get; set; }
        public int? SourceNumberOfRaces { get; set; }

        public IList<Season> SourceSeasons { get; set; }
    }
}