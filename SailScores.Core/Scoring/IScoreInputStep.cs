using System.Collections.Generic;
using SailScores.Core.Model;

namespace SailScores.Core.Scoring;

// Changes the scores before a run scores them (handicap time correction). It works on the
// run's copies, read with workspace.ScoresIn, never on the series' own scores.
public interface IScoreInputStep
{
    void Apply(ScoringWorkspace workspace, IEnumerable<Race> races);
}
