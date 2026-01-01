using SailScores.Core.Model;
using System.ComponentModel.DataAnnotations;
using SailScores.Api.Enumerations;
using System.Text;

namespace SailScores.Web.Models.SailScores;

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
    public String HomePageDescription { get; set; }
    public Guid? LogoFileId { get; set; }
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

    private DateTime recentCutoff = DateTime.Now.AddDays(-9);
    public IEnumerable<RaceSummaryViewModel> RecentRaces => Races?.Where(r => r.Date > recentCutoff
                                                                              && ((r.State ?? RaceState.Raced) == RaceState.Raced
                                                                                  || r.State == RaceState.Preliminary))
        .OrderByDescending(r => r.Date)
        .ThenBy(r => r.Order);

    public IEnumerable<SeriesSummary> RecentSeries => Series
        ?.Where(s =>
            (
                s.Type == SeriesType.Summary
                && (s.EndDate??DateOnly.MinValue) > DateOnly.FromDateTime(recentCutoff)
                && (s.UpdatedDate??DateTime.MinValue) > recentCutoff
                && (s.StartDate ?? DateOnly.MinValue) < DateOnly.FromDateTime(DateTime.Now)
            )
            || (s.Races
                ?.Any(r => r.Date > recentCutoff
                           && ((r.State ?? RaceState.Raced) == RaceState.Raced
                               || r.State == RaceState.Preliminary )) ?? false))
        .OrderByDescending(s => s.Races
            .Where(r => (r.State ?? RaceState.Raced) == RaceState.Raced
                        || r.State == RaceState.Preliminary).Max(r => r.Date))
        .ThenBy(s => s.Name);

    public IEnumerable<RaceSummaryViewModel> UpcomingRaces => Races?.Where(r =>
            r.Date >= DateTime.Today.AddDays(-1)
            && (r.State ?? RaceState.Raced) == RaceState.Scheduled)
        .OrderBy(r => r.Date)
        .ThenBy(r => r.Order)
        .ThenBy(r => r.FleetName)
        .Take(6);

    public IEnumerable<SeriesSummary> ImportantSeries => Series
        ?.Where(s =>
            s.IsImportantSeries ?? false)
        .OrderByDescending(s => s.Season.Name)
        .ThenBy(s => s.Name);

    public IEnumerable<RegattaSummaryViewModel> CurrentRegattas => Regattas
        ?.Where(r =>
            (r.EndDate.HasValue && r.EndDate.Value > DateTime.Today.AddDays(-14))
            &&
            (r.StartDate.HasValue && r.StartDate.Value < DateTime.Today.AddDays(14)))
        .OrderBy(r => r.Name);

    // aiming for a description with series names, but hopefully less than 200 characters.
    public String LongDescription
    {
        get
        {
            var longDescription = new StringBuilder();
            longDescription.Append($"Results for {Name} ({Initials})");

            var recentSeries = Series.Where(s => (s.UpdatedDate > (DateTime.Now.AddYears(-6)) && (s.IsImportantSeries ?? false)) ||
                s.UpdatedDate > (DateTime.Now.AddDays(-14)))
                .OrderBy( s => s.IsImportantSeries??false? 0:1).ThenByDescending(s => s.UpdatedDate);
            if (recentSeries.Any())
            {
                longDescription.Append(" including");

                var lastSeasonName = "";
                foreach(var series in recentSeries)
                {
                    if (lastSeasonName != series.Season.Name)
                    {
                        lastSeasonName = series.Season.Name;
                        longDescription.Append($" {series.Season.Name}:");
                    }
                    longDescription.Append($" {series.Name},");
                    if (longDescription.Length > 170)
                    {
                        break;
                    }
                }
                longDescription.Remove(longDescription.Length - 1, 1);
                longDescription.Append(".");
            }
            return longDescription.ToString();
        }
    }
}
#pragma warning restore CA2227 // Collection properties should be read only
