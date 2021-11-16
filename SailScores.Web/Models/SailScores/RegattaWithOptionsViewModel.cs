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

    public IEnumerable<Fleet> FleetOptions { get; internal set; }
    public IEnumerable<Guid> FleetIds { get; set; }
}