using System.Collections.Generic;
using System.Linq;

namespace SailScores.Core.Scoring.Tiebreaks;

// One step in ranking a series: decides between two competitors, or leaves them tied (0) for
// the next step. Negative when x ranks ahead of y. The workspace is the scoring run's.
public interface ITieBreak
{
    int Compare(ScoringWorkspace workspace, SeriesCompetitorResults x, SeriesCompetitorResults y);
}

// How a calculator orders competitors: its steps in turn, until one separates them.
//
//   Ranking.By(new LowestTotal(zeroTotalRanksLast: true))
//       .Then(new AppendixA81LowPoint())
//       .Then(new AppendixA82LowPoint())
public sealed class Ranking
{
    private readonly ITieBreak[] _steps;

    private Ranking(ITieBreak[] steps)
    {
        _steps = steps;
    }

    public static Ranking By(ITieBreak first) => new([first]);

    public Ranking Then(ITieBreak next) => new([.. _steps, next]);

    public IReadOnlyList<ITieBreak> Steps => _steps;

    public int Compare(ScoringWorkspace workspace, SeriesCompetitorResults x, SeriesCompetitorResults y)
    {
        foreach (var step in _steps)
        {
            var result = step.Compare(workspace, x, y);
            if (result != 0)
            {
                return result;
            }
        }
        return 0;
    }

    public override string ToString() => string.Join(", then ", _steps.Select(s => s.GetType().Name));
}
