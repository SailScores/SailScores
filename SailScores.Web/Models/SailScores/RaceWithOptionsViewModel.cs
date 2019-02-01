using SailScores.Core.Model;
using System;
using System.Collections.Generic;

namespace SailScores.Web.Models.SailScores
{
    public class RaceWithOptionsViewModel : Race
    {
        public IList<Fleet> FleetOptions { get; set; }
        public IList<Series> SeriesOptions { get; set; }
        public IList<ScoreCode> ScoreCodeOptions { get; set; }
        public IList<Competitor> CompetitorOptions { get; set; }

        public Guid FleetId { get; set; }
        public IList<Guid> SeriesIds { get; set; }
    }
}
