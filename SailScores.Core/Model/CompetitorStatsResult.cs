using System.Collections.Generic;

namespace SailScores.Core.Model;

public class CompetitorStatsResult
{
    public IList<CompetitorSeasonStats> SeasonStats { get; set; }

    public bool ClubHasHandicapScoring { get; set; }
    public bool ClubHasDefaultHandicapSystem { get; set; }
    public bool CompetitorHasRatingForDefaultSystem { get; set; }
    public string HandicapSystemName { get; set; }
}
