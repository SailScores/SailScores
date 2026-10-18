using System.Text.Json.Nodes;

namespace SailScores.FixtureTool;

// Lookups over a club backup, plus the shared (site-wide) systems the club's systems
// inherit from. Backups only hold the club's own systems.
internal sealed class BackupIndex
{
    public JsonObject Root { get; }
    public Dictionary<Guid, JsonObject> Series { get; }
    public Dictionary<Guid, JsonObject> Seasons { get; }
    public Dictionary<Guid, JsonObject> ScoringSystems { get; }
    public Dictionary<Guid, JsonObject> HandicapSystems { get; }
    public HashSet<Guid> SharedScoringSystemIds { get; } = [];
    public HashSet<Guid> SharedHandicapSystemIds { get; } = [];
    public ILookup<Guid, JsonObject> RacesBySeries { get; }

    public BackupIndex(JsonObject root, JsonObject? shared)
    {
        Root = root;
        Series = ById(Json.Objects(root, "series"));
        Seasons = ById(Json.Objects(root, "seasons"));
        ScoringSystems = ById(Json.Objects(root, "scoringSystems"));
        HandicapSystems = ById(Json.Objects(root, "handicapSystems"));

        foreach (var system in Json.Objects(shared, "scoringSystems"))
        {
            if (Json.Guid(system, "id") is Guid id && ScoringSystems.TryAdd(id, system))
            {
                SharedScoringSystemIds.Add(id);
            }
        }
        foreach (var system in Json.Objects(shared, "handicapSystems"))
        {
            if (Json.Guid(system, "id") is Guid id && HandicapSystems.TryAdd(id, system))
            {
                SharedHandicapSystemIds.Add(id);
            }
        }

        RacesBySeries = Json.Objects(root, "races")
            .SelectMany(race => Json.Guids(race, "seriesIds").Select(seriesId => (seriesId, race)))
            .ToLookup(p => p.seriesId, p => p.race);
    }

    // The series and all of its descendants, for summary series built from child series.
    public HashSet<Guid> SeriesWithDescendants(Guid seriesId)
    {
        var result = new HashSet<Guid>();
        var pending = new Stack<Guid>([seriesId]);
        while (pending.Count > 0)
        {
            var id = pending.Pop();
            if (!result.Add(id) || !Series.TryGetValue(id, out var series))
            {
                continue;
            }
            foreach (var child in Json.Guids(series, "childrenSeriesIds"))
            {
                pending.Push(child);
            }
        }
        result.IntersectWith(Series.Keys);
        return result;
    }

    // The system followed by its ancestors, nearest first. MissingId is set when the chain
    // reaches a system that isn't in the backup or the shared systems file.
    public (List<JsonObject> Chain, Guid? MissingId) ScoringChain(Guid? id) => Chain(ScoringSystems, id);

    public (List<JsonObject> Chain, Guid? MissingId) HandicapChain(Guid? id) => Chain(HandicapSystems, id);

    // The system the app scores a series with: its regatta's, then its own, then the
    // season default, then the club default.
    public Guid? EffectiveScoringSystemId(JsonObject series)
    {
        var seriesId = Json.Guid(series, "id");
        var regatta = Json.Objects(Root, "regattas")
            .FirstOrDefault(r => Json.Guids(r, "seriesIds").Contains(seriesId ?? Guid.Empty));
        return Json.Guid(regatta, "scoringSystemId")
            ?? Json.Guid(series, "scoringSystemId")
            ?? (Json.Guid(series, "seasonId") is Guid seasonId && Seasons.TryGetValue(seasonId, out var season)
                ? Json.Guid(season, "defaultScoringSystemId")
                : null)
            ?? ClubDefaultScoringSystemId;
    }

    // Backups name the club default rather than giving its id; restore matches on name.
    public Guid? ClubDefaultScoringSystemId =>
        Json.Guid(Root, "defaultScoringSystemId")
        ?? Json.Objects(Root, "scoringSystems")
            .Where(s => Json.String(s, "name") == Json.String(Root, "defaultScoringSystemName"))
            .Select(s => Json.Guid(s, "id"))
            .FirstOrDefault();

    public string SeasonUrlName(JsonObject series) =>
        Json.Guid(series, "seasonId") is Guid seasonId && Seasons.TryGetValue(seasonId, out var season)
            ? Json.String(season, "urlName") ?? ""
            : "";

    private static (List<JsonObject> Chain, Guid? MissingId) Chain(Dictionary<Guid, JsonObject> systems, Guid? id)
    {
        var chain = new List<JsonObject>();
        var seen = new HashSet<Guid>();
        while (id is Guid current && seen.Add(current))
        {
            if (!systems.TryGetValue(current, out var system))
            {
                return (chain, current);
            }
            chain.Add(system);
            id = Json.Guid(system, "parentSystemId");
        }
        return (chain, null);
    }

    private static Dictionary<Guid, JsonObject> ById(IEnumerable<JsonObject> items)
    {
        var result = new Dictionary<Guid, JsonObject>();
        foreach (var item in items)
        {
            if (Json.Guid(item, "id") is Guid id)
            {
                result[id] = item;
            }
        }
        return result;
    }
}
