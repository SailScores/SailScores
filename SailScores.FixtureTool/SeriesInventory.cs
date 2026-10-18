using System.Text;
using System.Text.Json.Nodes;

namespace SailScores.FixtureTool;

// One row per series describing the scoring features it exercises, for choosing a set of
// fixtures that covers every calculator, discard method and score code in use.
internal static class SeriesInventory
{
    private static readonly string[] Headers =
    [
        "Season", "Series", "Name", "Type", "Races", "ChildSeries", "DescendantRaces",
        "Competitors", "Fleets", "ScoringSystem", "Calculator", "DiscardMethod",
        "DiscardPattern", "ParticipationPercent", "Codes", "TiedPlaces", "HandicapSystem",
        "StoredResults", "SeriesId"
    ];

    public static string ToCsv(BackupIndex index)
    {
        var storedResults = Json.Objects(index.Root, "historicalResults")
            .Where(h => h["isCurrent"]?.GetValue<bool>() == true)
            .Select(h => Json.Guid(h, "seriesId"))
            .OfType<Guid>()
            .ToHashSet();

        var csv = new StringBuilder();
        csv.AppendLine(string.Join(",", Headers));

        var rows = index.Series.Values
            .OrderBy(index.SeasonUrlName)
            .ThenBy(s => Json.String(s, "urlName"));
        foreach (var series in rows)
        {
            var id = Json.Guid(series, "id")!.Value;
            var races = index.RacesBySeries[id].ToList();
            var allRaces = index.SeriesWithDescendants(id)
                .SelectMany(s => index.RacesBySeries[s])
                .Distinct()
                .ToList();
            var scores = allRaces.SelectMany(r => Json.Objects(r, "scores")).ToList();

            var (chain, missing) = index.ScoringChain(index.EffectiveScoringSystemId(series));
            var root = missing is null ? chain.LastOrDefault() : null;
            var handicap = Json.Guid(series, "handicapSystemId") is Guid handicapId
                && index.HandicapSystems.TryGetValue(handicapId, out var handicapSystem)
                    ? Json.String(handicapSystem, "name")
                    : null;

            csv.AppendLine(string.Join(",", new[]
            {
                index.SeasonUrlName(series),
                Json.String(series, "urlName"),
                Json.String(series, "name"),
                Json.Text(series, "type"),
                races.Count.ToString(),
                Json.Guids(series, "childrenSeriesIds").Count().ToString(),
                (allRaces.Count - races.Count).ToString(),
                scores.Select(s => Json.Guid(s, "competitorId")).Distinct().Count().ToString(),
                allRaces.Select(r => Json.Guid(r, "fleetId")).Distinct().Count().ToString(),
                chain.Count > 0 ? Json.String(chain[0], "name") : null,
                root is null ? "(missing base system: pass --shared)" : Json.Text(root, "calculatorType"),
                Nearest(chain, "discardMethod"),
                Nearest(chain, "discardPattern"),
                Nearest(chain, "participationPercent"),
                string.Join(" ", scores.Select(s => Json.String(s, "code")).OfType<string>()
                    .Where(c => c.Length > 0).Distinct().Order()),
                HasTiedPlaces(allRaces) ? "yes" : "",
                handicap,
                storedResults.Contains(id) ? "yes" : "",
                id.ToString()
            }.Select(Escape)));
        }
        return csv.ToString();
    }

    // The value a child system inherits: the nearest system in the chain that sets it.
    private static string? Nearest(List<JsonObject> chain, string name) =>
        chain.Select(s => Json.Text(s, name)).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static bool HasTiedPlaces(IEnumerable<JsonObject> races) =>
        races.Any(race => Json.Objects(race, "scores")
            .Where(s => string.IsNullOrEmpty(Json.String(s, "code")))
            .Select(s => Json.Text(s, "place"))
            .Where(p => p is not null && p != "null")
            .GroupBy(p => p)
            .Any(g => g.Count() > 1));

    private static string Escape(string? value)
    {
        value ??= "";
        return value.IndexOfAny([',', '"', '\n', '\r']) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
