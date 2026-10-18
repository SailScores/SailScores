using System;
using System.Globalization;
using System.Linq;
using SailScores.Core.Model;
using SailScores.Core.Scoring.Formulas;
using SailScores.Web.Models.SailScores;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring.Formulas;

// The formula set and each formula's step and glossary text. How formulas score is covered
// by the calculator and scenario tests (FormulaScenarioTests and others).
public class FormulaSetTests
{
    [Fact]
    public void Standard_HasEveryFormulaTheScoreCodeEditorOffers()
    {
        var offered = new ScoreCodeWithOptionsViewModel().FormulaOptions.Select(o => o.Value).Order();

        Assert.Equal(offered, FormulaSet.Standard.All.Select(f => f.Name).Order());
    }

    [Theory]
    [InlineData("FIX", FormulaStep.Race)]
    [InlineData("MAN", FormulaStep.Race)]
    [InlineData("FIN+", FormulaStep.Race)]
    [InlineData("CTS+", FormulaStep.Race)]
    [InlineData("PLC%", FormulaStep.Race)]
    [InlineData("SER+", FormulaStep.Series)]
    [InlineData("AVE", FormulaStep.Average)]
    [InlineData("AVE W", FormulaStep.Average)]
    [InlineData("AVE ND", FormulaStep.Average)]
    [InlineData("AVE W ND", FormulaStep.Average)]
    [InlineData("AVE P", FormulaStep.Average)]
    [InlineData("AVE W P", FormulaStep.Average)]
    [InlineData("TIE", FormulaStep.None)]
    [InlineData("COD", FormulaStep.None)]
    public void Find_StandardFormula_HasStep(string name, FormulaStep step)
    {
        Assert.Equal(step, FormulaSet.Standard.Find(name).Step);
    }

    [Fact]
    public void Find_LowerCase_FindsFormula()
    {
        Assert.IsType<AverageNonDiscardedFormula>(FormulaSet.Standard.Find("ave nd"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("XYZ")]
    public void Find_BlankOrUnknown_ReturnsNull(string name)
    {
        Assert.Null(FormulaSet.Standard.Find(name));
    }

    [Fact]
    public void With_Replacement_ReplacesOnlyThatFormula()
    {
        var replacement = new PlacePlusPercentFormula(PlacePlusPercentFormula.RoundingPre2025);

        var set = FormulaSet.Standard.With(replacement);

        Assert.Same(replacement, set.Find("PLC%"));
        Assert.NotSame(replacement, FormulaSet.Standard.Find("PLC%"));
        Assert.Equal(FormulaSet.Standard.All.Count(), set.All.Count());
    }

    [Fact]
    public void HighPoint_PlacePlusPercent_IsHighPointFormula()
    {
        Assert.IsType<HighPointPlacePlusPercentFormula>(FormulaSet.HighPoint.Find("PLC%"));
    }

    // Quirk: TIE is matched with case (docs/Scoring-Quirks.md).
    [Theory]
    [InlineData("TIE", true)]
    [InlineData("tie", false)]
    [InlineData(null, false)]
    public void IsTie_MatchesUpperCaseOnly(string formula, bool expected)
    {
        Assert.Equal(expected, TieFormula.IsTie(new ScoreCode { Formula = formula }));
    }

    [Theory]
    [InlineData("FIX", "3", "Fixed at 3")]
    [InlineData("MAN", null, "Manually entered value")]
    [InlineData("FIN+", "1", "Number of boats that finished race + 1")]
    [InlineData("FIN+", "0", "Number of boats that finished race")]
    [InlineData("CTS+", "1", "Number of boats that came to starting area + 1")]
    [InlineData("SER+", "2.5", "Number of boats entered in series + 2.5")]
    [InlineData("PLC%", "20", "Place + penalty (20% of DNF score)")]
    [InlineData("AVE", null, "Average of results")]
    [InlineData("AVE W", null, "Average of results rounded to a whole number")]
    [InlineData("AVE ND", null, "Average of non-discarded results")]
    [InlineData("AVE W ND", null, "Average of non-discarded results rounded to a whole number")]
    [InlineData("AVE P", null, "Average of results in prior races")]
    [InlineData("AVE W P", null, "Average of results in prior races rounded to a whole number")]
    [InlineData("TIE", null, "Average of tied places")]
    [InlineData("COD", null, "Scored like DNF")]
    public void Describe_StandardFormula_GivesGlossaryText(string name, string value, string expected)
    {
        var code = new ScoreCode
        {
            Formula = name,
            FormulaValue = value == null ? null : Decimal.Parse(value, CultureInfo.InvariantCulture),
            ScoreLike = "DNF"
        };

        Assert.Equal(expected, FormulaSet.Standard.Find(name).Describe(code));
    }
}
