using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddFrontPagePerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Index for Races table - optimizes date-based activity queries for recently active clubs
            // This index supports WHERE Date >= cutoffDate with efficient descending date ordering
            migrationBuilder.Sql(@"
                CREATE NONCLUSTERED INDEX [IX_Races_Date_ClubId] 
                ON [Races]([Date] DESC, [ClubId]) 
                INCLUDE ([Id]) 
                WHERE [Date] IS NOT NULL;
            ");

            // Index for Series table - optimizes UpdatedDate-based activity queries
            // This index supports WHERE UpdatedDate >= cutoffDate with efficient descending date ordering
            migrationBuilder.Sql(@"
                CREATE NONCLUSTERED INDEX [IX_Series_UpdatedDate_ClubId] 
                ON [Series]([UpdatedDateUtc] DESC, [ClubId]) 
                INCLUDE ([Id]) 
                WHERE [UpdatedDateUtc] IS NOT NULL;
            ");

            // Index for Clubs table - optimizes visibility filtering for front page
            // This index supports WHERE IsHidden = 0 with covering columns to avoid table lookups
            migrationBuilder.Sql(@"
                CREATE NONCLUSTERED INDEX [IX_Clubs_IsHidden_Id] 
                ON [Clubs]([IsHidden], [Id]) 
                INCLUDE ([Name], [Initials], [Description]) 
                WHERE [IsHidden] = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_Races_Date_ClubId] ON [Races];");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_Series_UpdatedDate_ClubId] ON [Series];");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_Clubs_IsHidden_Id] ON [Clubs];");
        }
    }
}
