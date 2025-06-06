using SailScores.Core.Model;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.SailScores;

#pragma warning disable CA2227 // Collection properties should be read only
public class RaceWithOptionsViewModel : RaceViewModel
{
    internal bool ClubHasCompetitors;

    public string ClubInitials { get; set; }
    public bool UseAdvancedFeatures { get; set; }
    public IList<Fleet> FleetOptions { get; set; }
    public IList<Series> SeriesOptions { get; set; }
    public IList<ScoreCode> ScoreCodeOptions { get; set; }
    public IList<Competitor> CompetitorOptions { get; set; }
    public IOrderedEnumerable<BoatClass> CompetitorBoatClassOptions { get; set; }

    public IList<KeyValuePair<string, string>> WeatherIconOptions { get; set; }

    [Required]
    public Guid FleetId { get; set; }
    public IList<Guid> SeriesIds { get; set; }

    public int? InitialOrder { get; set; }

    public new RegattaSummaryViewModel Regatta { get; set; }

    public Guid? RegattaId { get; set; }

    public IList<AdminToDoViewModel> Tips { get; set; }
    // help the client keep track of whether they might need to change the date.
    public bool? NeedsLocalDate { get; set; }
    public int? DefaultRaceDateOffset { get; set; }

}
#pragma warning restore CA2227 // Collection properties should be read only