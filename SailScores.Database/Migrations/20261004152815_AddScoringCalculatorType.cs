using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddScoringCalculatorType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CalculatorType",
                table: "ScoringSystems",
                type: "int",
                nullable: true);

            // Backfill base systems using the name rules ScoringCalculatorFactory used
            // before this migration, in the same order. Case-sensitive to match
            // String.Contains / StartsWith. Values match Api.Enumerations.ScoringCalculatorType.
            migrationBuilder.Sql(Deferred(@"
UPDATE ScoringSystems SET CalculatorType =
    CASE
        WHEN Name COLLATE Latin1_General_CS_AS LIKE '%High Point Percentage%' THEN 8
        WHEN Name COLLATE Latin1_General_CS_AS LIKE '%Cox-Sprague%' THEN 9
        WHEN Name COLLATE Latin1_General_CS_AS LIKE '%Low Point Average%'
            AND Name COLLATE Latin1_General_CS_AS LIKE '%without DNCs%' THEN 7
        WHEN Name COLLATE Latin1_General_CS_AS LIKE '%Low Point Average%' THEN 6
        WHEN Name COLLATE Latin1_General_CS_AS LIKE '%First = 0%' THEN 3
        WHEN Name COLLATE Latin1_General_CS_AS LIKE '%First = %'
            AND Name COLLATE Latin1_General_CS_AS LIKE '%.75%' THEN 5
        WHEN Name COLLATE Latin1_General_CS_AS LIKE '%First = %'
            AND Name COLLATE Latin1_General_CS_AS LIKE '%.7%' THEN 4
        WHEN Name COLLATE Latin1_General_CS_AS LIKE '%Top X High Point%' THEN 10
        WHEN Name COLLATE Latin1_General_CS_AS LIKE '%Appendix A%'
            AND Name COLLATE Latin1_General_CS_AS LIKE '%pre-2025%' THEN 2
        WHEN Name COLLATE Latin1_General_CS_AS LIKE 'PWA Standard%' THEN 11
        ELSE 1
    END
WHERE ParentSystemId IS NULL;"));
        }

        // Runs the statement through EXEC so SQL Server compiles it only when it runs. In an
        // idempotent script (dotnet ef migrations script --idempotent) it shares a batch with
        // the ALTER TABLE that adds the column it uses, and would otherwise fail to compile.
        private static string Deferred(string sql) => "EXEC(N'" + sql.Replace("'", "''") + "');";

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CalculatorType",
                table: "ScoringSystems");
        }
    }
}
