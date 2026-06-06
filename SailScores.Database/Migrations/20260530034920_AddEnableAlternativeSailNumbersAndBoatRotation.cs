using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddEnableAlternativeSailNumbersAndBoatRotation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableAlternativeSailNumbers",
                table: "Clubs",
                type: "bit",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BoatRotations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClubId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BoatSailNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BoatClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RotationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoatRotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoatRotations_BoatClasses_BoatClassId",
                        column: x => x.BoatClassId,
                        principalTable: "BoatClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BoatRotations_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BoatRotations_Competitors_CompetitorId",
                        column: x => x.CompetitorId,
                        principalTable: "Competitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoatRotations_BoatClassId",
                table: "BoatRotations",
                column: "BoatClassId");

            migrationBuilder.CreateIndex(
                name: "IX_BoatRotations_ClubId_RotationDate",
                table: "BoatRotations",
                columns: new[] { "ClubId", "RotationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BoatRotations_CompetitorId",
                table: "BoatRotations",
                column: "CompetitorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoatRotations");

            migrationBuilder.DropColumn(
                name: "EnableAlternativeSailNumbers",
                table: "Clubs");
        }
    }
}
