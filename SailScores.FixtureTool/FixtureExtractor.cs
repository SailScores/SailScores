using System.Text.Json.Nodes;

namespace SailScores.FixtureTool;

public sealed record FixtureResult(JsonObject Fixture, IReadOnlyList<string> Warnings);

// Cuts a club backup down to what one series needs to be scored: the series and its
// descendants, their races and scores, the competitors, fleets, classes and seasons
// involved, the scoring and handicap systems with all their ancestors, and the stored
// current results. The result is still a valid club backup, so the existing restore
// code can load it.
internal sealed class FixtureExtractor(BackupIndex index)
{
    // Built from the whole backup, so a competitor has the same name in every fixture.
    private readonly Dictionary<Guid, string> _anonymousNames =
        NameAnonymizer.BuildNames(Json.Objects(index.Root, "competitors"));

    // Collections with nothing to do with scoring.
    private static readonly string[] DroppedCollections =
    [
        "announcements", "documents", "files", "clubSequences", "competitorForwarders",
        "regattaForwarders", "seriesForwarders", "seriesChartResults", "changeTypes",
        "competitorChanges"
    ];

    public FixtureResult Extract(Guid seriesId)
    {
        var warnings = new List<string>();
        // Skip the dropped collections when copying: documents and files can be large.
        var fixture = new JsonObject();
        foreach (var (name, value) in index.Root.Where(p => !DroppedCollections.Contains(p.Key)))
        {
            fixture[name] = value?.DeepClone();
        }

        var seriesIds = index.SeriesWithDescendants(seriesId);

        var raceIds = new HashSet<Guid>();
        var competitorIds = new HashSet<Guid>();
        var fleetIds = new HashSet<Guid>();
        foreach (var race in seriesIds.SelectMany(id => index.RacesBySeries[id]).Distinct())
        {
            raceIds.Add(Json.Guid(race, "id")!.Value);
            if (Json.Guid(race, "fleetId") is Guid fleetId)
            {
                fleetIds.Add(fleetId);
            }
            foreach (var score in Json.Objects(race, "scores"))
            {
                if (Json.Guid(score, "competitorId") is Guid competitorId)
                {
                    competitorIds.Add(competitorId);
                }
            }
        }

        var series = seriesIds.Select(id => index.Series[id]).ToList();
        fleetIds.UnionWith(series.Select(s => Json.Guid(s, "fleetId")).OfType<Guid>());

        var regattas = Json.Objects(index.Root, "regattas")
            .Where(r => Json.Guids(r, "seriesIds").Any(seriesIds.Contains))
            .ToList();
        var regattaIds = regattas.Select(r => Json.Guid(r, "id")!.Value).ToHashSet();
        foreach (var regatta in regattas)
        {
            fleetIds.UnionWith(Json.Guids(regatta, "fleetIds"));
        }

        var fleets = Json.Objects(index.Root, "fleets")
            .Where(f => Json.Guid(f, "id") is Guid id && fleetIds.Contains(id))
            .ToList();
        competitorIds.UnionWith(FleetMembers(fleets));

        var competitors = Json.Objects(index.Root, "competitors")
            .Where(c => competitorIds.Contains(Json.Guid(c, "id")!.Value))
            .ToList();
        var boatClassIds = competitors.Select(c => Json.Guid(c, "boatClassId")).OfType<Guid>()
            .Concat(fleets.SelectMany(f => Json.Guids(f, "boatClassIds")))
            .ToHashSet();

        var seasonIds = series.Select(s => Json.Guid(s, "seasonId"))
            .Concat(regattas.Select(r => Json.Guid(r, "seasonId")))
            .OfType<Guid>()
            .ToHashSet();

        // Every system a series might fall back to: regatta, series, season default, club default.
        var scoringSystemIds = CollectScoringSystems(
            series.Select(s => Json.Guid(s, "scoringSystemId"))
                .Concat(regattas.Select(r => Json.Guid(r, "scoringSystemId")))
                .Concat(seasonIds.Select(id => Json.Guid(index.Seasons.GetValueOrDefault(id), "defaultScoringSystemId")))
                .Append(index.ClubDefaultScoringSystemId),
            warnings);

        // Handicap systems are few and a fleet can name its default by name, so keep them all.
        var handicapSystemIds = CollectHandicapSystems(warnings);

        // Filter the collections.
        Json.Filter(fixture, "series", s => seriesIds.Contains(Json.Guid(s, "id")!.Value));
        Json.Filter(fixture, "races", r => raceIds.Contains(Json.Guid(r, "id")!.Value));
        Json.Filter(fixture, "regattas", r => regattaIds.Contains(Json.Guid(r, "id")!.Value));
        Json.Filter(fixture, "fleets", f => fleetIds.Contains(Json.Guid(f, "id")!.Value));
        Json.Filter(fixture, "competitors", c => competitorIds.Contains(Json.Guid(c, "id")!.Value));
        Json.Filter(fixture, "boatClasses", b => boatClassIds.Contains(Json.Guid(b, "id")!.Value));
        Json.Filter(fixture, "seasons", s => seasonIds.Contains(Json.Guid(s, "id")!.Value));
        Json.Filter(fixture, "scoringSystems", s => scoringSystemIds.Contains(Json.Guid(s, "id")!.Value));
        Json.Filter(fixture, "handicapSystems", h => handicapSystemIds.Contains(Json.Guid(h, "id")!.Value));
        Json.Filter(fixture, "competitorHandicaps", h => competitorIds.Contains(Json.Guid(h, "competitorId") ?? Guid.Empty));
        Json.Filter(fixture, "historicalResults", h =>
            seriesIds.Contains(Json.Guid(h, "seriesId") ?? Guid.Empty)
            && h["isCurrent"]?.GetValue<bool>() == true);

        AddSharedSystems(fixture, "scoringSystems", index.SharedScoringSystemIds, scoringSystemIds, index.ScoringSystems);
        AddSharedSystems(fixture, "handicapSystems", index.SharedHandicapSystemIds, handicapSystemIds, index.HandicapSystems);

        // Trim id lists that point outside the fixture.
        foreach (var s in Json.Objects(fixture, "series"))
        {
            Json.FilterIds(s, "parentSeriesIds", seriesIds);
            Json.FilterIds(s, "childrenSeriesIds", seriesIds);
        }
        foreach (var r in Json.Objects(fixture, "races"))
        {
            Json.FilterIds(r, "seriesIds", seriesIds);
        }
        foreach (var r in Json.Objects(fixture, "regattas"))
        {
            Json.FilterIds(r, "seriesIds", seriesIds);
            Json.FilterIds(r, "fleetIds", fleetIds);
        }
        foreach (var c in Json.Objects(fixture, "competitors"))
        {
            Json.FilterIds(c, "fleetIds", fleetIds);
        }

        StripUnscoredData(fixture);
        NameAnonymizer.Apply(fixture, _anonymousNames);

        var stripped = Scrubber.RemoveUserFields(fixture) + ScrubStoredResults(fixture);
        var redacted = Scrubber.RedactEmails(fixture);
        if (redacted > 0)
        {
            warnings.Add($"Redacted {redacted} other value(s) that looked like email addresses.");
        }

        UpdateMetadataCounts(fixture);
        var target = index.Series[seriesId];
        fixture["fixtureInfo"] = new JsonObject
        {
            ["seriesId"] = seriesId.ToString(),
            ["seriesName"] = Json.String(target, "name"),
            ["seasonUrlName"] = index.SeasonUrlName(target),
            ["seriesUrlName"] = Json.String(target, "urlName"),
            ["sourceBackupCreatedUtc"] = Json.String(index.Root["metadata"], "createdDateUtc"),
            ["extractedUtc"] = DateTime.UtcNow.ToString("O"),
            ["userFieldsRemoved"] = stripped,
            ["warnings"] = new JsonArray(warnings.Select(w => (JsonNode?)JsonValue.Create(w)).ToArray())
        };

        return new FixtureResult(fixture, warnings);
    }

    // Competitors a fleet includes even when they have no scores, which matters to
    // calculators that score fleet members who never raced.
    private IEnumerable<Guid> FleetMembers(List<JsonObject> fleets)
    {
        var competitors = Json.Objects(index.Root, "competitors").ToList();
        foreach (var fleet in fleets)
        {
            var fleetId = Json.Guid(fleet, "id")!.Value;
            var classIds = Json.Guids(fleet, "boatClassIds").ToHashSet();
            var type = Json.Text(fleet, "fleetType");
            IEnumerable<JsonObject> members = type switch
            {
                "AllBoatsInClub" or "1" => competitors,
                "SelectedClasses" or "2" => competitors.Where(c => Json.Guid(c, "boatClassId") is Guid b && classIds.Contains(b)),
                _ => competitors.Where(c => Json.Guids(c, "fleetIds").Contains(fleetId))
            };
            foreach (var member in members)
            {
                yield return Json.Guid(member, "id")!.Value;
            }
        }
    }

    private HashSet<Guid> CollectScoringSystems(IEnumerable<Guid?> startIds, List<string> warnings)
    {
        var ids = new HashSet<Guid>();
        foreach (var startId in startIds.OfType<Guid>().Distinct())
        {
            var (chain, missing) = index.ScoringChain(startId);
            ids.UnionWith(chain.Select(s => Json.Guid(s, "id")!.Value));
            if (missing is Guid missingId)
            {
                warnings.Add($"Scoring system {missingId} is not in the backup or the shared systems file; "
                    + "the fixture will not score correctly. Pass --shared.");
            }
        }
        return ids;
    }

    private HashSet<Guid> CollectHandicapSystems(List<string> warnings)
    {
        var ids = new HashSet<Guid>();
        foreach (var system in Json.Objects(index.Root, "handicapSystems"))
        {
            var (chain, missing) = index.HandicapChain(Json.Guid(system, "id"));
            ids.UnionWith(chain.Select(s => Json.Guid(s, "id")!.Value));
            if (missing is Guid missingId)
            {
                warnings.Add($"Handicap system {missingId} is not in the backup or the shared systems file.");
            }
        }
        return ids;
    }

    private static void AddSharedSystems(
        JsonObject fixture,
        string collection,
        HashSet<Guid> sharedIds,
        HashSet<Guid> neededIds,
        Dictionary<Guid, JsonObject> systems)
    {
        var toAdd = neededIds.Where(sharedIds.Contains).ToList();
        if (toAdd.Count == 0)
        {
            return;
        }
        if (fixture[collection] is not JsonArray array)
        {
            fixture[collection] = array = [];
        }
        // Shared systems go first, as base systems are listed before the club's own.
        foreach (var id in toAdd)
        {
            array.Insert(0, systems[id].DeepClone());
        }
    }

    // Free text and other data the calculators never read.
    private static void StripUnscoredData(JsonObject fixture)
    {
        Json.Remove(fixture, "description", "homePageDescription", "statisticsDescription",
            "weatherSettings", "logoFileId", "url");
        Json.RemoveFromAll(Json.Objects(fixture, "series"), "description");
        Json.RemoveFromAll(Json.Objects(fixture, "races"), "description", "trackingUrl", "weather");
        Json.RemoveFromAll(Json.Objects(fixture, "regattas"), "description", "url");
        Json.RemoveFromAll(Json.Objects(fixture, "fleets"), "description");
        Json.RemoveFromAll(Json.Objects(fixture, "boatClasses"), "description");
        Json.RemoveFromAll(Json.Objects(fixture, "competitors"), "notes");
        Json.RemoveFromAll(Json.Objects(fixture, "competitorHandicaps"), "notes");
        Json.RemoveFromAll(Json.Objects(fixture, "handicapSystems"), "description");
    }

    private static int ScrubStoredResults(JsonObject fixture)
    {
        var changes = 0;
        foreach (var results in Json.Objects(fixture, "historicalResults"))
        {
            if (Json.String(results, "results") is string json)
            {
                results["results"] = Scrubber.ScrubEmbeddedJson(json, out var count);
                changes += count;
            }
        }
        return changes;
    }

    private static void UpdateMetadataCounts(JsonObject fixture)
    {
        if (fixture["metadata"] is not JsonObject metadata)
        {
            return;
        }
        metadata["boatClassCount"] = Json.Count(fixture, "boatClasses");
        metadata["competitorCount"] = Json.Count(fixture, "competitors");
        metadata["fleetCount"] = Json.Count(fixture, "fleets");
        metadata["raceCount"] = Json.Count(fixture, "races");
        metadata["scoreCount"] = Json.Objects(fixture, "races").Sum(r => Json.Count(r, "scores"));
        metadata["seasonCount"] = Json.Count(fixture, "seasons");
        metadata["seriesCount"] = Json.Count(fixture, "series");
        metadata["regattaCount"] = Json.Count(fixture, "regattas");
        metadata["scoringSystemCount"] = Json.Count(fixture, "scoringSystems");
    }
}
