using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddClassHandicap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClassHandicaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BoatClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandicapSystemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassHandicaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassHandicaps_BoatClasses_BoatClassId",
                        column: x => x.BoatClassId,
                        principalTable: "BoatClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassHandicaps_HandicapSystems_HandicapSystemId",
                        column: x => x.HandicapSystemId,
                        principalTable: "HandicapSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassHandicap_NullEnd",
                table: "ClassHandicaps",
                columns: new[] { "BoatClassId", "HandicapSystemId" },
                unique: true,
                filter: "[EffectiveTo] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ClassHandicaps_HandicapSystemId",
                table: "ClassHandicaps",
                column: "HandicapSystemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassHandicaps");
        }
    }
}
