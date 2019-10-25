using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using System;
using System.Collections.Generic;

namespace SailScores.Web.Models.SailScores
{
    public class RaceWithOptionsViewModel : RaceViewModel
    {
        public IList<Fleet> FleetOptions { get; set; }
        public IList<Series> SeriesOptions { get; set; }
        public IList<ScoreCode> ScoreCodeOptions { get; set; }
        public IList<Competitor> CompetitorOptions { get; set; }

        public Guid FleetId { get; set; }
        public IList<Guid> SeriesIds { get; set; }

        public int? InitialOrder { get; set; }

        public RegattaSummaryViewModel Regatta { get; set; }

        public Guid? RegattaId { get; set; }

        public IList<AdminToDoViewModel> Tips { get; set; }
    }
}
