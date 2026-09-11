using SailScores.Core.Model;
using System.Collections.Generic;
using System.Linq;

namespace SailScores.Core.Scoring;

public class AppliedScoreCodeGroupSummary
{
    public string Description { get; set; }

    public static AppliedScoreCodeGroupSummary FromGroup(ScoreCodeGroup group)
    {
        var codeList = (group.IncludedCodeNames ?? [])
            .OrderBy(c => c)
            .ToList();

        var codesText = string.Join(", ", codeList);
        var codeLabel = codeList.Count == 1 ? "score code is" : "score codes are";
        var limitText = GetLimitText(group);

        return new AppliedScoreCodeGroupSummary
        {
            Description = $"{codesText} {codeLabel} only allowed for {limitText}"
        };
    }

    private static string GetLimitText(ScoreCodeGroup group)
    {
        return group.LimitationType switch
        {
            ScoreCodeGroupLimitationType.NumberOfDates =>
                $"{group.LimitationValue:0.###} {(group.LimitationValue == 1m ? "day" : "days")}",
            ScoreCodeGroupLimitationType.PercentOfRaces =>
                $"{group.LimitationValue:0.###}% of {(group.UseNonDiscardedRaces ? "non-discarded" : "all")} races",
            _ =>
                $"{group.LimitationValue:0.###} {(group.LimitationValue == 1m ? "race" : "races")}"
        };
    }
}
