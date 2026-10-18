using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using SailScores.Core.FlatModel;

namespace SailScores.Test.Unit.Core.Scoring.Characterization;

// A readable, stable rendering of series results for snapshot comparison. Ids are left out
// (restore assigns new ones each run), as are timestamps and the trend, which depends on
// earlier results.
//
// Each competitor's race scores are one string, in race order:
//   3          place 3, scored 3
//   2:2.5      place 2, scored 2.5 (a tie, for example)
//   DNC 12     code DNC, scored 12
//   (DNC 12)   discarded
//   7/9        percentage systems: scored 7 of a possible 9
//   -          no score in that race
public static class ResultsSnapshot
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string ToJson(FlatResults results)
    {
        var races = results.Races.ToList();
        var competitors = results.Competitors.ToDictionary(c => c.Id);

        var snapshot = new
        {
            results.ScoringSystemName,
            results.TotalHeader,
            DiscardMethod = results.DiscardMethod?.ToString(),
            results.NumberOfDiscards,
            results.NumberOfSailedRaces,
            results.IsPreliminary,
            results.IsPercentSystem,
            results.LowerScoreWins,
            PercentRequired = Format(results.PercentRequired),
            Columns = results.Columns?.Select(c => $"{c.Key}: {c.Header} ({c.Format}, {c.Visibility})").ToList(),
            Races = races.Select(Describe).ToList(),
            Competitors = results.CalculatedScores
                .Select(s => (Score: s, Competitor: competitors[s.CompetitorId]))
                .OrderBy(x => x.Score.Rank ?? int.MaxValue)
                .ThenBy(x => x.Competitor.Name, StringComparer.Ordinal)
                .ThenBy(x => x.Competitor.SailNumber, StringComparer.Ordinal)
                .Select(x => new
                {
                    x.Score.Rank,
                    x.Competitor.Name,
                    x.Competitor.SailNumber,
                    Total = Format(x.Score.TotalScore),
                    TotalIncludingDiscards = Format(x.Score.TotalScoreIncludingDiscards),
                    Average = Format(x.Score.Average),
                    PointsEarned = Format(x.Score.PointsEarned),
                    PointsPossible = Format(x.Score.PointsPossible),
                    ParticipationPercent = Format(x.Score.ParticipationPercent),
                    Columns = x.Score.ColumnValues?.Count > 0
                        ? x.Score.ColumnValues.OrderBy(c => c.Key, StringComparer.Ordinal)
                            .ToDictionary(c => c.Key, c => Describe(c.Value))
                        : null,
                    Scores = string.Join(" | ", races.Select(r =>
                        Describe(x.Score.Scores?.FirstOrDefault(s => s.RaceId == r.Id), results.IsPercentSystem)))
                })
                .ToList(),
            CodesUsed = results.ScoreCodesUsed?.Count > 0
                ? results.ScoreCodesUsed.OrderBy(c => c.Key, StringComparer.Ordinal)
                    .ToDictionary(c => c.Key, c => $"{c.Value.Formula}: {c.Value.Description}")
                : null,
            CodeGroups = results.AppliedScoreCodeGroupSummaries?.Count > 0
                ? results.AppliedScoreCodeGroupSummaries.Select(g => g.Description).ToList()
                : null
        };

        return JsonSerializer.Serialize(snapshot, Options);
    }

    private static string Describe(FlatRace race)
    {
        var date = race.Date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            ?? race.StartDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var parts = new[]
        {
            $"{race.Order}.",
            date,
            race.Name,
            race.IsSeries == true ? $"[series, {race.TotalChildRaceCount} races]" : null,
            race.State is { } state && state != SailScores.Api.Enumerations.RaceState.Raced ? $"[{state}]" : null
        };
        return string.Join(" ", parts.Where(p => !string.IsNullOrEmpty(p)));
    }

    private static string Describe(FlatCalculatedScore score, bool isPercentSystem)
    {
        if (score == null)
        {
            return "-";
        }
        var value = Format(score.ScoreValue) ?? "null";
        var text = !string.IsNullOrEmpty(score.Code)
            ? $"{score.Code} {value}"
            : score.Place.HasValue && score.Place.Value != score.ScoreValue
                ? $"{score.Place}:{value}"
                : value;
        if (isPercentSystem && score.PerfectScoreValue.HasValue)
        {
            text += $"/{Format(score.PerfectScoreValue)}";
        }
        return score.Discard ? $"({text})" : text;
    }

    private static string Describe(SailScores.Core.Scoring.ScoringColumnValue value) =>
        value.Multiplier.HasValue
            ? $"{Format(value.Value)} x {Format(value.Multiplier)}"
            : Format(value.Value) ?? "null";

    // Trailing zeros dropped, so 8.40 and 8.4 snapshot the same.
    public static string Format(decimal? value) =>
        value?.ToString("0.##########", CultureInfo.InvariantCulture);
}
