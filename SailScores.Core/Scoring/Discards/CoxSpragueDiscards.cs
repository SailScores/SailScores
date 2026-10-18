using System.Linq;

namespace SailScores.Core.Scoring.Discards;

// Cox-Sprague: the discard pattern is applied to the races the competitor sailed (started
// or counting as participation). Each discard is the race whose removal raises the
// competitor's percentage most: the race scoring furthest below the competitor's percentage
// of its perfect score. Only races they came to the start in can go. Child series discards
// and discard groups aren't used.
//
// Quirk: a code that can't be discarded isn't protected; the code built a discardable-only
// list but never used it (docs/Scoring-Quirks.md).
public sealed class CoxSpragueDiscards : IDiscardRule
{
    public void Apply(ScoringWorkspace workspace, SeriesResults results, SeriesCompetitorResults competitor)
    {
        var host = workspace.Host;
        var racesSailed = competitor.CalculatedScores
            .Count(s => host.CountsAsStarted(s.Value.RawScore) || host.CountsAsParticipation(s.Value.RawScore));
        int numOfDiscards = host.DiscardsForRaceCount(racesSailed);

        for (int i = 0; i < numOfDiscards; i++)
        {
            var race = NextRaceToDiscard(host, results, competitor);
            if (race == null)
            {
                break;
            }
            competitor.CalculatedScores[race].Discard = true;
        }
    }

    // For each candidate race: score − perfect score × (competitor's points ÷ perfect points),
    // over races not yet discarded. The most negative goes.
    private static Model.Race NextRaceToDiscard(IScoringHost host, SeriesResults results, SeriesCompetitorResults competitor)
    {
        decimal perfectTotal = competitor.CalculatedScores.Values
            .Where(s => !s.Discard && host.CameToStart(s.RawScore)).Sum(s => s.PerfectScoreValue ?? 0);
        decimal compScore = competitor.CalculatedScores.Values
            .Where(s => !s.Discard && host.CameToStart(s.RawScore)).Sum(s => s.ScoreValue ?? 0);

        var candidateRaceIds = competitor.CalculatedScores.Values
            .Where(s => !s.Discard && host.CameToStart(s.RawScore))
            .Select(s => s.RawScore.RaceId);
        var candidateRaces = results.Races.Where(r => candidateRaceIds.Any(id => id == r.Id));

        Model.Race raceToDiscard = null;
        decimal discardQuotient = 0;
        foreach (var race in candidateRaces)
        {
            decimal racePerfectScore = competitor.CalculatedScores[race].PerfectScoreValue ?? 0;
            decimal raceScore = competitor.CalculatedScores[race].ScoreValue ?? 0;

            decimal quotient = raceScore - (racePerfectScore * compScore / perfectTotal);
            if (quotient < discardQuotient)
            {
                raceToDiscard = race;
                discardQuotient = quotient;
            }
        }
        return raceToDiscard;
    }
}
