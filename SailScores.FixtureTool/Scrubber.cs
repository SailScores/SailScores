using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace SailScores.FixtureTool;

// Removes the personal data a backup carries: the user names (email addresses) in the
// who-changed-it fields. Anything else that looks like an email address is redacted as
// a safety net.
internal static partial class Scrubber
{
    private static readonly HashSet<string> UserFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "createdBy", "updatedBy", "changedBy"
    };

    public const string Redacted = "redacted@example.com";

    [GeneratedRegex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}")]
    private static partial Regex EmailPattern();

    public static int RemoveUserFields(JsonNode? node)
    {
        var removed = 0;
        switch (node)
        {
            case JsonObject obj:
                foreach (var name in obj.Select(p => p.Key).Where(UserFields.Contains).ToList())
                {
                    obj.Remove(name);
                    removed++;
                }
                foreach (var child in obj.Select(p => p.Value).ToList())
                {
                    removed += RemoveUserFields(child);
                }
                break;
            case JsonArray array:
                foreach (var child in array)
                {
                    removed += RemoveUserFields(child);
                }
                break;
        }
        return removed;
    }

    public static int RedactEmails(JsonNode? node)
    {
        var redacted = 0;
        switch (node)
        {
            case JsonObject obj:
                foreach (var (name, child) in obj.ToList())
                {
                    if (TryRedact(child, out var replacement))
                    {
                        obj[name] = replacement;
                        redacted++;
                    }
                    else
                    {
                        redacted += RedactEmails(child);
                    }
                }
                break;
            case JsonArray array:
                for (var i = 0; i < array.Count; i++)
                {
                    if (TryRedact(array[i], out var replacement))
                    {
                        array[i] = replacement;
                        redacted++;
                    }
                    else
                    {
                        redacted += RedactEmails(array[i]);
                    }
                }
                break;
        }
        return redacted;
    }

    // Stored results are JSON inside a string property, so they need their own pass.
    public static string ScrubEmbeddedJson(string json, out int changes)
    {
        var node = JsonNode.Parse(json);
        if (node is null)
        {
            changes = 0;
            return json;
        }
        changes = RemoveUserFields(node) + RedactEmails(node);
        return changes == 0 ? json : Json.ToCompactString(node);
    }

    private static bool TryRedact(JsonNode? node, out JsonNode? replacement)
    {
        replacement = null;
        if (node is JsonValue value && value.TryGetValue(out string? text)
            && text != Redacted && EmailPattern().IsMatch(text))
        {
            replacement = JsonValue.Create(EmailPattern().Replace(text, Redacted));
            return true;
        }
        return false;
    }
}
