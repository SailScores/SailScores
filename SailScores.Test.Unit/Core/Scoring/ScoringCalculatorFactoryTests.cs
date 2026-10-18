using Microsoft.Extensions.Caching.Memory;
using SailScores.Api.Enumerations;
using SailScores.Core.Scoring;
using SailScores.Database;
using SailScores.Test.Unit.Utilities;
using System;
using System.Threading.Tasks;
using Xunit;
using Db = SailScores.Database.Entities;
using Model = SailScores.Core.Model;

namespace SailScores.Test.Unit.Core.Scoring;

public class ScoringCalculatorFactoryTests
{
    private readonly ISailScoresContext _context;
    private readonly ScoringCalculatorFactory _factory;

    public ScoringCalculatorFactoryTests()
    {
        _context = InMemoryContextBuilder.GetContext();
        _factory = new ScoringCalculatorFactory(_context, new MemoryCache(new MemoryCacheOptions()));
    }

    private static string Describe(IScoringCalculator calculator) =>
        Assert.IsType<ScoringPipeline>(calculator).Recipe.Name;

    [Theory]
    [InlineData(null, "Appendix A")]
    [InlineData(ScoringCalculatorType.AppendixA, "Appendix A")]
    [InlineData(ScoringCalculatorType.AppendixAPre2025, "Appendix A (pre-2025)")]
    [InlineData(ScoringCalculatorType.AppAAltFirstIsZero, "Appendix A, First = 0")]
    [InlineData(ScoringCalculatorType.AppAAltFirstIsPoint7, "Appendix A, First = .7")]
    [InlineData(ScoringCalculatorType.AppAAltFirstIsPoint75, "Appendix A, First = .75")]
    [InlineData(ScoringCalculatorType.LowPointAverageInclDnc, "Low Point Average")]
    [InlineData(ScoringCalculatorType.LowPointAverageExclDnc, "Low Point Average without DNCs")]
    [InlineData(ScoringCalculatorType.HighPointPercentage, "High Point Percentage")]
    [InlineData(ScoringCalculatorType.CoxSprague, "Cox-Sprague")]
    [InlineData(ScoringCalculatorType.TopXHighPoint, "Top X High Point")]
    [InlineData(ScoringCalculatorType.PwaStandard, "PWA Standard")]
    [InlineData(ScoringCalculatorType.Wawasee2026, "Wawasee 2026")]
    [InlineData(ScoringCalculatorType.AppendixBWindsurfing, "Appendix B Windsurfing")]
    public async Task CreateScoringCalculatorAsync_WithCalculatorType_ReturnsMatchingCalculator(
        ScoringCalculatorType? calculatorType,
        string expected)
    {
        var system = new Model.ScoringSystem
        {
            Id = Guid.NewGuid(),
            Name = "Name should not matter",
            CalculatorType = calculatorType
        };

        var calculator = await _factory.CreateScoringCalculatorAsync(system);

        Assert.Equal(expected, Describe(calculator));
    }

    [Fact]
    public async Task CreateScoringCalculatorAsync_ChildWithoutResolvedType_UsesRootSystemType()
    {
        var root = new Db.ScoringSystem
        {
            Id = Guid.NewGuid(),
            Name = "Some base system",
            CalculatorType = ScoringCalculatorType.CoxSprague
        };
        var middle = new Db.ScoringSystem
        {
            Id = Guid.NewGuid(),
            Name = "Appendix A middle",
            ParentSystemId = root.Id
        };
        _context.ScoringSystems.Add(root);
        _context.ScoringSystems.Add(middle);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var child = new Model.ScoringSystem
        {
            Id = Guid.NewGuid(),
            Name = "Low Point Average child",
            ParentSystemId = middle.Id
        };

        var calculator = await _factory.CreateScoringCalculatorAsync(child);

        Assert.Equal("Cox-Sprague", Describe(calculator));
    }
}
