using System;
using System.Collections.Generic;
using System.Linq;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using SailScores.Core.Model.BackupEntities;
using SailScores.Test.Unit.Core.Scoring.Characterization;

namespace SailScores.Test.Unit.Core.Scoring.Scenarios;

// The site's shared base scoring systems (Fixtures/shared-systems.json) as model objects,
// resolved the way ScoringService resolves them for a calculator.
public static class SharedBaseSystems
{
    private static readonly Lazy<IReadOnlyList<ScoringSystemBackup>> Systems =
        new(() => ScoringFixture.SharedSystems().Where(s => s.ParentSystemId == null).ToList());

    public static IEnumerable<string> Names => Systems.Value.Select(s => s.Name);

    // The base system for a calculator. Where there are several (Appendix A has regatta and
    // series versions), the first by name.
    public static ScoringSystem Create(ScoringCalculatorType calculatorType) =>
        ToModel(Systems.Value
            .Where(s => s.CalculatorType == calculatorType)
            .OrderBy(s => s.Name, StringComparer.Ordinal)
            .FirstOrDefault()
            ?? throw new ArgumentException($"No shared base system for {calculatorType} in shared-systems.json."));

    public static ScoringSystem Create(string name) =>
        ToModel(Systems.Value.SingleOrDefault(s => s.Name == name)
            ?? throw new ArgumentException($"No shared base system named \"{name}\" in shared-systems.json."));

    // A new copy each call, so a scenario can change it freely.
    private static ScoringSystem ToModel(ScoringSystemBackup backup)
    {
        var settings = ScoringSystemSettings.FromJson(backup.Settings);
        return new ScoringSystem
        {
            Id = Guid.NewGuid(),
            Name = backup.Name,
            DiscardPattern = backup.DiscardPattern,
            ParticipationPercent = backup.ParticipationPercent,
            CalculatorType = backup.CalculatorType,
            Settings = settings,
            EffectiveSettings = ScoringSystemSettings.FromJson(backup.Settings),
            DiscardMethod = backup.DiscardMethod,
            EffectiveDiscardMethod = backup.DiscardMethod ?? DiscardMethod.RaceCountPattern,
            ScoreCodes = (backup.ScoreCodes ?? []).Select(ToModel).ToList(),
            InheritedScoreCodes = [],
            ScoreCodeGroups = [],
            InheritedScoreCodeGroups = []
        };
    }

    private static ScoreCode ToModel(ScoreCodeBackup code) => new()
    {
        Id = Guid.NewGuid(),
        Name = code.Name,
        Description = code.Description,
        Formula = code.Formula,
        FormulaValue = code.FormulaValueDecimal ?? code.FormulaValue,
        ScoreLike = code.ScoreLike,
        Discardable = code.Discardable,
        CameToStart = code.CameToStart,
        Started = code.Started,
        Finished = code.Finished,
        PreserveResult = code.PreserveResult,
        AdjustOtherScores = code.AdjustOtherScores,
        CountAsParticipation = code.CountAsParticipation,
        IncludeInAverage = code.IncludeInAverage
    };
}
