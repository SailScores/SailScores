using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class CompCustomFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableCustomCompetitorFields",
                table: "Clubs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CompetitorFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClubId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayHeader = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitorFieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetitorFieldDefinitions_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompetitorFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitorFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetitorFieldValues_CompetitorFieldDefinitions_FieldDefinitionId",
                        column: x => x.FieldDefinitionId,
                        principalTable: "CompetitorFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompetitorFieldValues_Competitors_CompetitorId",
                        column: x => x.CompetitorId,
                        principalTable: "Competitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SeriesResultsTemplateCustomFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeriesResultsTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Visibility = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesResultsTemplateCustomFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeriesResultsTemplateCustomFields_CompetitorFieldDefinitions_FieldDefinitionId",
                        column: x => x.FieldDefinitionId,
                        principalTable: "CompetitorFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeriesResultsTemplateCustomFields_SeriesResultsTemplates_SeriesResultsTemplateId",
                        column: x => x.SeriesResultsTemplateId,
                        principalTable: "SeriesResultsTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompetitorFieldDefinition_ClubOrder",
                table: "CompetitorFieldDefinitions",
                columns: new[] { "ClubId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CompetitorFieldValue_CompetitorField",
                table: "CompetitorFieldValues",
                columns: new[] { "CompetitorId", "FieldDefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_CompetitorFieldValues_FieldDefinitionId",
                table: "CompetitorFieldValues",
                column: "FieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_SeriesResultsTemplateCustomFields_FieldDefinitionId",
                table: "SeriesResultsTemplateCustomFields",
                column: "FieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateCustomField_TemplateField",
                table: "SeriesResultsTemplateCustomFields",
                columns: new[] { "SeriesResultsTemplateId", "FieldDefinitionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompetitorFieldValues");

            migrationBuilder.DropTable(
                name: "SeriesResultsTemplateCustomFields");

            migrationBuilder.DropTable(
                name: "CompetitorFieldDefinitions");

            migrationBuilder.DropColumn(
                name: "EnableCustomCompetitorFields",
                table: "Clubs");
        }
    }
}
