using System;
using System.Globalization;
using System.Linq;
using SailScores.Core.Model;
using SailScores.Core.Scoring;

namespace SailScores.Test.Unit.Core.Scoring.Scenarios;

// A scored scenario: the calculator's results, looked up by boat name.
public sealed class ScenarioResults(Series series, SeriesResults results)
{
    public Series Series { get; } = series;
    public SeriesResults Results { get; } = results;

    // Series competitors only: with a fleet, boats outside it aren't scored.
    public Competitor Competitor(string name) => Series.Competitors.Single(c => c.Name == name);

    public SeriesCompetitorResults For(string name) => Results.Results[Competitor(name)];

    // Race numbers count from 1, in the order the scenario added them.
    public Race Race(int number) => Series.Races[number - 1];

    public CalculatedScore Score(string name, int raceNumber) =>
        For(name).CalculatedScores.TryGetValue(Race(raceNumber), out var score) ? score : null;

    // One boat's race scores in race order, in the snapshot format:
    //   3  place 3 scored 3 | 2:2.5  place 2 scored 2.5 | DNC 12  code | (…) discarded | -  none
    public string Scores(string name) =>
        String.Join(" ", Series.Races.Select((_, i) => Describe(Score(name, i + 1))));

    private static string Describe(CalculatedScore score)
    {
        if (score == null)
        {
            return "-";
        }
        var value = Format(score.ScoreValue) ?? "null";
        var text = !String.IsNullOrEmpty(score.Code)
            ? $"{score.Code} {value}"
            : score.Place is { } place && place != score.ScoreValue
                ? $"{place}:{value}"
                : value;
        return score.Discard ? $"({text})" : text;
    }

    public static string Format(decimal? value) =>
        value?.ToString("0.##########", CultureInfo.InvariantCulture);
}
