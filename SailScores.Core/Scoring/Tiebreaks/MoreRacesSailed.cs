using System.Linq;

namespace SailScores.Core.Scoring.Tiebreaks;

// High point systems: more races with a place, or with a code that counts as participation,
// ranks ahead.
public sealed class MoreRacesSailed : ITieBreak
{
    public int Compare(ScoringWorkspace workspace, SeriesCompetitorResults x, SeriesCompetitorResults y) =>
        RacesSailed(y).CompareTo(RacesSailed(x));

    private static int RacesSailed(SeriesCompetitorResults results) =>
        results.CalculatedScores.Count(s =>
            (s.Value.RawScore?.Place.HasValue ?? false)
            || (s.Value.CountsAsParticipation ?? false));
}
