using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    // DiscardPattern and ParticipationPercent now inherit from the parent system when blank.
    // Before, a blank value meant no discards / no participation requirement (Top X High
    // Point: count 5 races). So that existing results don't change, child systems that are
    // blank but have an ancestor with a value get that old meaning written explicitly.
    public partial class InheritDiscardPatternAndParticipation : Migration
    {
        // Each child system paired with each of its ancestors.
        private const string AncestorChain = @"
WITH Chain AS (
    SELECT s.[Id] AS SystemId, s.[ParentSystemId] AS AncestorId, 1 AS Depth
    FROM [ScoringSystems] s
    WHERE s.[ParentSystemId] IS NOT NULL
    UNION ALL
    SELECT c.SystemId, a.[ParentSystemId], c.Depth + 1
    FROM Chain c
    JOIN [ScoringSystems] a ON a.[Id] = c.AncestorId
    WHERE a.[ParentSystemId] IS NOT NULL AND c.Depth < 20
)";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(AncestorChain + @"
UPDATE s SET [DiscardPattern] = '0'
FROM [ScoringSystems] s
WHERE NULLIF(LTRIM(RTRIM(s.[DiscardPattern])), '') IS NULL
    AND EXISTS (
        SELECT 1 FROM Chain c
        JOIN [ScoringSystems] a ON a.[Id] = c.AncestorId
        WHERE c.SystemId = s.[Id]
            AND NULLIF(LTRIM(RTRIM(a.[DiscardPattern])), '') IS NOT NULL);");

            migrationBuilder.Sql(AncestorChain + @"
UPDATE s SET [ParticipationPercent] =
    CASE WHEN EXISTS (
            SELECT 1 FROM Chain c
            JOIN [ScoringSystems] a ON a.[Id] = c.AncestorId
            WHERE c.SystemId = s.[Id] AND a.[ParentSystemId] IS NULL AND a.[CalculatorType] = 10)
        THEN 5 ELSE 0 END
FROM [ScoringSystems] s
WHERE s.[ParticipationPercent] IS NULL
    AND EXISTS (
        SELECT 1 FROM Chain c
        JOIN [ScoringSystems] a ON a.[Id] = c.AncestorId
        WHERE c.SystemId = s.[Id] AND a.[ParticipationPercent] IS NOT NULL);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The explicit values written by Up give the same results as before either way.
        }
    }
}
