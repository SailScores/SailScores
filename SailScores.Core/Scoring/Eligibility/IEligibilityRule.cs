namespace SailScores.Core.Scoring.Eligibility;

// Decides who can be ranked, after totals: a competitor who isn't eligible has no total. A
// calculator applies its eligibility rules in order.
public interface IEligibilityRule
{
    void Apply(ScoringWorkspace workspace, SeriesResults results);
}
