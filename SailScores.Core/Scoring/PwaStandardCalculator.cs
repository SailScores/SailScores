using SailScores.Core.Model;

namespace SailScores.Core.Scoring;

// even though this class inherits from AppAAltFirstisPoint7
// and then AppendixACalculator,
// it is still considered a "Base" system: it does not inherit
// scoring codes from any system and in the database, the parent
// system is null.

public class PwaStandardCalculator : AppAAltFirstIsPoint7
{
    public PwaStandardCalculator(ScoringSystem scoringSystem) : base(scoringSystem)
    {
        CompetitorComparer = new PwaSeriesCompComparer();
    }

}
