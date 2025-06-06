﻿using SailScores.ImportExport.Sailwave.Elements.File;
using SailScores.ImportExport.Sailwave.Parsers;
using Xunit;

namespace SailScores.ImportExport.Sailwave.Test.Unit;

public class ScoreCodeParserTests
{
    private FileRow GetSimpleScoreCodeRow()
    {
        return new FileRow
        {
            CompetitorOrScoringSystemId = null,
            Name = "scrcode",
            Value =
                "BFD|Score like|DNF|Yes|Yes||spare|spare|spare|spare|spare|No|No|No|5||Black flag disqualification under rule 30.3",
            RaceId = null,
            RowType = RowType.ScoringSystem
        };
    }

    [Fact]
    public void ParseSimpleRow()
    {
        var code = ScoreCodeParser.GetCode(GetSimpleScoreCodeRow());

        Assert.NotNull(code);
        Assert.Equal(5, code.ScoringSystemId);
    }

}
