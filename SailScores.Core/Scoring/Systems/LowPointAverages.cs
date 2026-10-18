using SailScores.Core.Model;
using SailScores.Core.Scoring.Adjustments;
using SailScores.Core.Scoring.Discards;
using SailScores.Core.Scoring.Eligibility;
using SailScores.Core.Scoring.Glossary;
using SailScores.Core.Scoring.RacePoints;
using SailScores.Core.Scoring.Tiebreaks;
using SailScores.Core.Scoring.Totals;

namespace SailScores.Core.Scoring.Systems;

// Low point average: the series score is the boat's average race score, so boats that sail
// different numbers of races can be compared.
//   - Points are places, counting the boats ahead (ties share the average of the places).
//   - No score is worse than DNC. Scores a code says aren't averaged count as 0, which the
//     average leaves out.
//   - The total is the average of the scores not discarded; boats short of the
//     participation percent aren't ranked.
//   - Low average wins; ties by A8.1, then A8.2.
public static class LowPointAverages
{
    // Every score, DNC included, counts in the average. Discards follow the pattern over the
    // series' races.
    public static ScoringRecipe IncludingDnc(ScoringSystem system) => new()
    {
        Name = "Low Point Average",
        RacePoints = LowPointPlaces.LowPointAverage,
        Adjustments = [new CapAtDefaultScore(), new ZeroWhenNotAveraged()],
        Discards = [new WorstScoresByPattern()],
        Total = new LowPointAverage(),
        Eligibility = [new ParticipationPercent()],
        Ranking = Rankings.LowPoint
    };

    // DNC and similar codes (didn't come to the start, scored from a boat count) aren't
    // averaged. Discards follow the pattern over the races each boat sailed, worst first.
    public static ScoringRecipe ExcludingDnc(ScoringSystem system) => new()
    {
        Name = "Low Point Average without DNCs",
        RacePoints = LowPointPlaces.LowPointAverage,
        AveragedScores = AveragedScores.ExceptCountsNotAtStart,
        Adjustments = [new CapAtDefaultScore(), new ZeroWhenNotAveraged()],
        Discards = [new WorstPercentagesByPattern(lowerScoreWins: true, countAveragedRaces: true)],
        Total = new LowPointAverage(),
        Eligibility = [new ParticipationPercent()],
        Ranking = Rankings.LowPoint,
        Glossary = new NotAveragedExcludedGlossary(AveragedScores.ExceptCountsNotAtStart)
    };

    // PBG: low point average without DNCs. A summary series using its child series' races
    // keeps the discards each child series made; any other series uses the discard pattern
    // (the system's discard method, ChildSeriesDiscardsOrPattern). Participation leaves out
    // discarded races: races sailed and not discarded, over the races not discarded.
    public static ScoringRecipe Pbg(ScoringSystem system) => new()
    {
        Name = "PBG",
        RacePoints = LowPointPlaces.LowPointAverage,
        AveragedScores = AveragedScores.ExceptCountsNotAtStart,
        Adjustments = [new CapAtDefaultScore(), new ZeroWhenNotAveraged()],
        Discards = [new WorstPercentagesByPattern(lowerScoreWins: true, countAveragedRaces: true)],
        Total = new LowPointAverage(),
        Eligibility = [new ParticipationPercent(), new ParticipationOfRacesNotDiscarded()],
        Ranking = Rankings.LowPoint,
        Glossary = new NotAveragedExcludedGlossary(AveragedScores.ExceptCountsNotAtStart)
    };
}
