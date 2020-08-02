using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using SailScores.Api.Enumerations;

namespace SailScores.Web.Models.SailScores
{

#pragma warning disable CA2227 // Collection properties should be read only
    public class ClubSummaryViewModel
    {
        public Guid Id { get; set; }

        public bool CanEdit { get; set; }

        [Required]
        [StringLength(200)]
        public String Name { get; set; }
        [StringLength(10)]
        public String Initials { get; set; }
        public String Description { get; set; }
        public bool IsHidden { get; set; }
        public String Url { get; set; }

        public IList<Fleet> Fleets { get; set; }
        public IList<Competitor> Competitors { get; set; }
        public IList<BoatClass> BoatClasses { get; set; }
        public IList<Season> Seasons { get; set; }
        public IList<SeriesSummary> Series { get; set; }
        public IList<RaceSummaryViewModel> Races { get; set; }
        public IList<RegattaSummaryViewModel> Regattas { get; set; }

        public ScoringSystem DefaultScoringSystem { get; set; }

        public IList<ScoringSystem> ScoringSystems { get; set; }

        private DateTime recentCutoff = DateTime.Now.AddDays(-8);
        public IEnumerable<RaceSummaryViewModel> RecentRaces => Races?.Where(r => r.Date > recentCutoff
            && (r.State ?? RaceState.Raced) == RaceState.Raced)
            .OrderByDescending(r => r.Date)
            .ThenBy(r => r.Order);

        public IEnumerable<SeriesSummary> RecentSeries => Series
                ?.Where(s =>
                    s.Races
                    ?.Any(r => r.Date > recentCutoff
                        && (r.State ?? RaceState.Raced) == RaceState.Raced) ?? false)
            .OrderByDescending(s => s.Races.Where(r => (r.State ?? RaceState.Raced) == RaceState.Raced).Max(r => r.Date))
            .ThenBy(s => s.Name);

        public IEnumerable<RaceSummaryViewModel> UpcomingRaces => Races?.Where(r => r.Date >= DateTime.Today.AddDays(-1)
            && (r.State ?? RaceState.Raced) == RaceState.Scheduled)
            .OrderBy(r => r.Date)
            .ThenBy(r => r.Order)
            .ThenBy(r => r.FleetName)
            .Take(6);

        public IEnumerable<SeriesSummary> ImportantSeries => Series
                ?.Where(s =>
                    s.IsImportantSeries ?? false)
            .OrderBy(s => s.Season.Name)
            .ThenBy(s => s.Name);

        public IEnumerable<RegattaSummaryViewModel> CurrentRegattas => Regattas
        ?.Where(r =>
            (r.EndDate.HasValue && r.EndDate.Value > DateTime.Today.AddDays(-14))
            &&
            (r.StartDate.HasValue && r.StartDate.Value < DateTime.Today.AddDays(14)))
        .OrderBy(r => r.Name);
    }
#pragma warning restore CA2227 // Collection properties should be read only
}
