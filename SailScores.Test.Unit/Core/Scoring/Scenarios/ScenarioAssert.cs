using System;
using System.Linq;
using Xunit;

namespace SailScores.Test.Unit.Core.Scoring.Scenarios;

// Assertions on scenario results, with messages that show the whole picture on failure.
public static class ScenarioAssert
{
    // Boats in rank order. Boats left unranked (no rank) aren't listed.
    public static void Ranks(ScenarioResults results, params string[] namesInRankOrder)
    {
        var actual = results.Series.Competitors
            .Select(c => (c.Name, Rank: results.Results.Results[c].Rank))
            .Where(x => x.Rank.HasValue)
            .OrderBy(x => x.Rank)
            .ThenBy(x => x.Name, StringComparer.Ordinal)
            .Select(x => x.Name)
            .ToArray();
        Assert.True(namesInRankOrder.SequenceEqual(actual),
            $"Expected ranks {String.Join(", ", namesInRankOrder)}; got {String.Join(", ", actual)}.\n{Table(results)}");
    }

    public static void Rank(ScenarioResults results, string name, int? rank) =>
        Assert.True(results.For(name).Rank == rank,
            $"Expected {name} rank {rank?.ToString() ?? "none"}; got {results.For(name).Rank?.ToString() ?? "none"}.\n{Table(results)}");

    public static void Totals(ScenarioResults results, params (string Name, decimal? Total)[] expected)
    {
        foreach (var (name, total) in expected)
        {
            Assert.True(results.For(name).TotalScore == total,
                $"Expected {name} total {ScenarioResults.Format(total) ?? "none"}; "
                + $"got {ScenarioResults.Format(results.For(name).TotalScore) ?? "none"}.\n{Table(results)}");
        }
    }

    // A boat's race scores in ScenarioResults.Scores format, e.g. "1 (DNC 5) 2:2.5".
    public static void Scores(ScenarioResults results, string name, string expected) =>
        Assert.True(results.Scores(name) == expected,
            $"Expected {name} scores \"{expected}\"; got \"{results.Scores(name)}\".\n{Table(results)}");

    // Every boat: rank, name, total, race scores.
    public static string Table(ScenarioResults results) =>
        String.Join("\n", results.Series.Competitors
            .OrderBy(c => results.Results.Results[c].Rank ?? Int32.MaxValue)
            .Select(c =>
            {
                var r = results.Results.Results[c];
                return $"  {r.Rank?.ToString() ?? "-",3} {c.Name,-8} {ScenarioResults.Format(r.TotalScore) ?? "-",8}  {results.Scores(c.Name)}";
            }));
}
