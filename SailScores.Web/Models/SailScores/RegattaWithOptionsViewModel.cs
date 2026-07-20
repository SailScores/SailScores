using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

public class RegattaWithOptionsViewModel : Core.Model.Regatta
{
    public IEnumerable<Season> SeasonOptions { get; set; }

    public IList<ScoringSystem> ScoringSystemOptions { get; set; }

    private Guid _seasonId;
    public Guid SeasonId
    {
        get
        {
            if (this.Season != null)
            {
                return this.Season.Id;
            }
            return _seasonId;
        }
        set
        {
            _seasonId = value;
        }

    }

    private IEnumerable<Fleet> _fleetOptions = System.Array.Empty<Fleet>();

    public IEnumerable<Fleet> FleetOptions
    {
        get => _fleetOptions ?? System.Array.Empty<Fleet>();
        internal set => _fleetOptions = value ?? System.Array.Empty<Fleet>();
    }

    public IEnumerable<Guid> FleetIds { get; set; }
}
