using SailScores.Core.Model;
using SailScores.Core.Scoring.Adjustments;
using SailScores.Core.Scoring.Discards;
using SailScores.Core.Scoring.Eligibility;
using SailScores.Core.Scoring.RacePoints;
using SailScores.Core.Scoring.Tiebreaks;
using SailScores.Core.Scoring.Totals;

namespace SailScores.Core.Scoring.Systems;

// PWA (windsurfing) standard scoring: Appendix A with first scoring 0.7, ranked by the PWA
// tiebreaks: beaten the other more often, better scores including discards, better discard,
// then the last race.
public static class PwaStandard
{
    public static ScoringRecipe Create(ScoringSystem system) => new()
    {
        Name = "PWA Standard",
        RacePoints = LowPointPlaces.FirstIsPoint7,
        Adjustments = [new CapAtDefaultScore()],
        Discards = [new WorstScoresByPattern()],
        Total = new SumOfScores(),
        Eligibility = [new ParticipationPercent()],
        Ranking = Rankings.Pwa
    };
}
