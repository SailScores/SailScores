using System.Data;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;

namespace SailScores.FixtureTool;

// Exports the shared (site-wide, ClubId null) scoring and handicap systems. Club backups
// leave them out, but club systems inherit their calculator type, settings and score
// codes from them, so a fixture needs them to score.
//
// Reads every column, so the output follows whatever schema the database has. Systems
// are written in the club backup's format; score code groups use the same field names
// as the ScoreCodeGroup entity, with codes as a list of code names.
internal static class SharedSystemsExporter
{
    public static JsonObject Export(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        var scoringSystems = Query(connection,
            "SELECT * FROM ScoringSystems WHERE ClubId IS NULL ORDER BY Name",
            "ClubId", "IsRestrictedToClubs");
        var systemIds = scoringSystems.Select(s => Json.Guid(s, "id")).ToHashSet();

        var scoreCodes = Query(connection,
            "SELECT sc.* FROM ScoreCodes sc JOIN ScoringSystems ss ON ss.Id = sc.ScoringSystemId "
            + "WHERE ss.ClubId IS NULL ORDER BY sc.Name");
        foreach (var code in scoreCodes)
        {
            // Backups write the decimal value as formulaValue, falling back to the old int.
            if (code["formulaValueDecimal"] is JsonNode value)
            {
                code["formulaValue"] = value.DeepClone();
            }
        }

        var groups = TableExists(connection, "ScoreCodeGroups")
            ? Query(connection,
                "SELECT g.* FROM ScoreCodeGroups g JOIN ScoringSystems ss ON ss.Id = g.ScoringSystemId "
                + "WHERE ss.ClubId IS NULL ORDER BY g.Name")
            : [];
        var groupCodes = TableExists(connection, "ScoreCodeGroupCodes")
            ? Query(connection, "SELECT ScoreCodeGroupId, CodeName FROM ScoreCodeGroupCodes ORDER BY CodeName")
            : [];
        foreach (var group in groups)
        {
            var groupId = Json.Guid(group, "id");
            group["codes"] = new JsonArray(groupCodes
                .Where(c => Json.Guid(c, "scoreCodeGroupId") == groupId)
                .Select(c => (JsonNode?)JsonValue.Create(Json.String(c, "codeName")))
                .ToArray());
        }

        foreach (var system in scoringSystems)
        {
            var id = Json.Guid(system, "id");
            system["scoreCodes"] = Children(scoreCodes, id);
            system["scoreCodeGroups"] = Children(groups, id);
        }

        var handicapSystems = Query(connection,
            "SELECT * FROM HandicapSystems WHERE ClubId IS NULL ORDER BY Name",
            "ClubId");

        return new JsonObject
        {
            ["exportedUtc"] = DateTime.UtcNow.ToString("O"),
            ["scoringSystems"] = new JsonArray(scoringSystems.ToArray<JsonNode?>()),
            ["handicapSystems"] = new JsonArray(handicapSystems.ToArray<JsonNode?>())
        };
    }

    // Replaces the given scoring systems in an earlier export with their exported copies,
    // adding any that are new in name order, and leaves everything else as it was. This
    // updates one system from a database that doesn't have all the others (a local
    // database without the club-restricted systems, say).
    public static void Merge(JsonObject existing, JsonObject exported, IReadOnlyCollection<Guid> systemIds)
    {
        var systems = existing["scoringSystems"] as JsonArray
            ?? throw new InvalidDataException("The existing file has no scoringSystems.");
        foreach (var id in systemIds)
        {
            var system = Json.Objects(exported, "scoringSystems").FirstOrDefault(s => Json.Guid(s, "id") == id)
                ?? throw new ArgumentException($"No shared scoring system with id {id} in the database.");
            var copy = system.DeepClone();

            var index = systems.OfType<JsonObject>().ToList().FindIndex(s => Json.Guid(s, "id") == id);
            if (index >= 0)
            {
                systems[index] = copy;
                continue;
            }

            var name = Json.String(system, "name");
            var insertAt = systems.OfType<JsonObject>().ToList()
                .FindIndex(s => StringComparer.OrdinalIgnoreCase.Compare(Json.String(s, "name"), name) > 0);
            systems.Insert(insertAt < 0 ? systems.Count : insertAt, copy);
        }
    }

    private static JsonArray Children(List<JsonObject> rows, Guid? systemId)
    {
        var children = rows.Where(r => Json.Guid(r, "scoringSystemId") == systemId)
            .Select(r =>
            {
                var copy = r.DeepClone().AsObject();
                copy.Remove("scoringSystemId");
                return (JsonNode?)copy;
            });
        return new JsonArray(children.ToArray());
    }

    private static bool TableExists(SqlConnection connection, string table)
    {
        using var command = new SqlCommand("SELECT OBJECT_ID(@name, 'U')", connection);
        command.Parameters.AddWithValue("@name", table);
        return command.ExecuteScalar() is not (null or DBNull);
    }

    // Column names as backup property names: FormulaValue_Decimal becomes formulaValueDecimal.
    private static string PropertyName(string column)
    {
        var name = column.Replace("_", "");
        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    // Rows as JSON objects with camelCase names. Nulls are left out, as in backups.
    private static List<JsonObject> Query(SqlConnection connection, string sql, params string[] skipColumns)
    {
        using var command = new SqlCommand(sql, connection);
        using var reader = command.ExecuteReader();
        var rows = new List<JsonObject>();
        while (reader.Read())
        {
            var row = new JsonObject();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var column = reader.GetName(i);
                if (reader.IsDBNull(i) || skipColumns.Contains(column, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }
                row[PropertyName(column)] = reader.GetValue(i) switch
                {
                    Guid g => JsonValue.Create(g.ToString()),
                    DateTime d => JsonValue.Create(d.ToString("O")),
                    decimal m => JsonValue.Create(m),
                    double f => JsonValue.Create(f),
                    float f => JsonValue.Create(f),
                    long n => JsonValue.Create(n),
                    int n => JsonValue.Create(n),
                    short n => JsonValue.Create(n),
                    byte n => JsonValue.Create(n),
                    bool b => JsonValue.Create(b),
                    var other => JsonValue.Create(Convert.ToString(other))
                };
            }
            rows.Add(row);
        }
        return rows;
    }
}
