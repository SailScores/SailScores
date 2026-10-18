using System.IO.Compression;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SailScores.FixtureTool;

// Helpers for working with backups as raw JSON. Backups use camelCase names; enums may
// be written as names or numbers.
internal static class Json
{
    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly JsonSerializerOptions CompactOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static JsonObject ReadFile(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length >= 2 && bytes[0] == 0x1f && bytes[1] == 0x8b)
        {
            using var input = new MemoryStream(bytes);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            bytes = output.ToArray();
        }

        var offset = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;

        return JsonNode.Parse(bytes.AsSpan(offset)) as JsonObject
            ?? throw new InvalidDataException($"{path} does not contain a JSON object.");
    }

    public static void WriteFile(string path, JsonNode node, bool gzip)
    {
        var text = node.ToJsonString(IndentedOptions);
        if (!gzip)
        {
            File.WriteAllText(path, text);
            return;
        }

        using var file = File.Create(path);
        using var compressed = new GZipStream(file, CompressionLevel.Optimal);
        using var writer = new StreamWriter(compressed);
        writer.Write(text);
    }

    public static string ToCompactString(JsonNode node) => node.ToJsonString(CompactOptions);

    public static IEnumerable<JsonObject> Objects(JsonNode? node, string name) =>
        (node?[name] as JsonArray)?.OfType<JsonObject>() ?? [];

    public static string? String(JsonNode? node, string name) =>
        node?[name] is JsonValue value && value.TryGetValue(out string? text) ? text : null;

    // Enum values come back as their name or their number; either way, as text.
    public static string? Text(JsonNode? node, string name) =>
        node?[name] is JsonValue value
            ? value.TryGetValue(out string? text) ? text : value.ToJsonString()
            : null;

    public static Guid? Guid(JsonNode? node, string name) =>
        System.Guid.TryParse(String(node, name), out var id) ? id : null;

    public static IEnumerable<Guid> Guids(JsonNode? node, string name) =>
        (node?[name] as JsonArray)?
            .Select(n => System.Guid.TryParse(n is JsonValue v && v.TryGetValue(out string? s) ? s : null, out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
        ?? [];

    // Keeps only the array elements matching the predicate. A missing array stays missing.
    public static void Filter(JsonObject parent, string name, Func<JsonObject, bool> keep)
    {
        if (parent[name] is not JsonArray array)
        {
            return;
        }

        for (var i = array.Count - 1; i >= 0; i--)
        {
            if (array[i] is not JsonObject item || !keep(item))
            {
                array.RemoveAt(i);
            }
        }
    }

    // Keeps only the ids in an id list that are in the given set.
    public static void FilterIds(JsonObject parent, string name, ISet<Guid> keep)
    {
        if (parent[name] is not JsonArray array)
        {
            return;
        }

        for (var i = array.Count - 1; i >= 0; i--)
        {
            var text = array[i] is JsonValue v && v.TryGetValue(out string? s) ? s : null;
            if (!System.Guid.TryParse(text, out var id) || !keep.Contains(id))
            {
                array.RemoveAt(i);
            }
        }
    }

    public static int Count(JsonNode? node, string name) => (node?[name] as JsonArray)?.Count ?? 0;

    public static void Remove(JsonObject obj, params string[] names)
    {
        foreach (var name in names)
        {
            obj.Remove(name);
        }
    }

    public static void RemoveFromAll(IEnumerable<JsonObject> objects, params string[] names)
    {
        foreach (var obj in objects)
        {
            Remove(obj, names);
        }
    }
}
