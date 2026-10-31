namespace SailScores.Core.Scoring.RacePoints;

// Scores coded results in place of the race step of score code formulas (a recipe without
// one evaluates FIX, MAN, FIN+, CTS+, STR+ and PLC% there).
public interface ICodedResultsRule
{
    void Apply(ScoringWorkspace workspace, SeriesResults results, SeriesCompetitorResults competitor);
}
