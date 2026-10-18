using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace SailScores.FixtureTool;

// Replaces competitor names with "Comp" and the initials of each word, in the order written
// ("Smith, Jane" -> CompSJ, "Jones/Brown, Ann/Bob" -> CompJBAB). Repeated initials are
// numbered (CompSJ2). URL names made from the name are replaced too, and the
// stored results' copies are renamed to match by id. Race descriptions in the stored
// results are removed too: they're free text (race officers' names, for example), and the
// fixture's own races already lose theirs. Names already in this form are kept, so running
// it twice changes nothing.
internal static partial class NameAnonymizer
{
    public const string Prefix = "Comp";

    [GeneratedRegex(@"\p{L}+")]
    private static partial Regex Words();

    [GeneratedRegex(@"^Comp[A-Z]*\d*$")]
    private static partial Regex Anonymized();

    [GeneratedRegex(@"[^a-z0-9]")]
    private static partial Regex NotSlug();

    // New names by competitor id, numbered in the order the competitors are listed.
    public static Dictionary<Guid, string> BuildNames(IEnumerable<JsonObject> competitors)
    {
        var names = new Dictionary<Guid, string>();
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var competitor in competitors)
        {
            if (Json.Guid(competitor, "id") is not Guid id || Json.String(competitor, "name") is not string name)
            {
                continue;
            }
            if (Anonymized().IsMatch(name))
            {
                used.Add(name);
                names[id] = name;
                continue;
            }
            var initials = string.Concat(Words().Matches(name).Select(m => char.ToUpperInvariant(m.Value[0])));
            var candidate = Prefix + initials;
            for (var n = 2; used.Contains(candidate); n++)
            {
                candidate = Prefix + initials + n;
            }
            used.Add(candidate);
            names[id] = candidate;
        }
        return names;
    }

    // Returns the number of competitors renamed and race descriptions removed.
    public static int Apply(JsonObject fixture, IReadOnlyDictionary<Guid, string> names)
    {
        var renamed = 0;
        foreach (var competitor in Json.Objects(fixture, "competitors"))
        {
            if (Rename(competitor, "id", "name", "urlName", names))
            {
                renamed++;
            }
        }
        foreach (var results in Json.Objects(fixture, "historicalResults"))
        {
            if (Json.String(results, "results") is not string json || JsonNode.Parse(json) is not JsonObject stored)
            {
                continue;
            }
            var changed = false;
            foreach (var competitor in Json.Objects(stored, "Competitors"))
            {
                changed |= Rename(competitor, "Id", "Name", "UrlName", names);
            }
            foreach (var race in Json.Objects(stored, "Races"))
            {
                if (race["Description"] != null)
                {
                    race.Remove("Description");
                    changed = true;
                    renamed++;
                }
            }
            if (changed)
            {
                results["results"] = Json.ToCompactString(stored);
            }
        }
        return renamed;
    }

    private static bool Rename(JsonObject competitor, string idField, string nameField, string urlField,
        IReadOnlyDictionary<Guid, string> names)
    {
        if (Json.Guid(competitor, idField) is not Guid id || !names.TryGetValue(id, out var newName)
            || Json.String(competitor, nameField) is not string oldName || oldName == newName)
        {
            return false;
        }
        competitor[nameField] = newName;
        if (Json.String(competitor, urlField) is string urlName && Slug(urlName) == Slug(oldName))
        {
            competitor[urlField] = newName.ToLowerInvariant();
        }
        return true;
    }

    private static string Slug(string text) => NotSlug().Replace(text.ToLowerInvariant(), "");
}
