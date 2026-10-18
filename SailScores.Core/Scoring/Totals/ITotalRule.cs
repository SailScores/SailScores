using System.Collections.Generic;
using SailScores.Core.Model;

namespace SailScores.Core.Scoring.Totals;

// Works out every competitor's series total (and average, points and display flags).
public interface ITotalRule
{
    void Apply(ScoringWorkspace workspace, SeriesResults results, IEnumerable<Score> scores);
}
