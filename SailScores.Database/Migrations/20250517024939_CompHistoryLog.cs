using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class CompHistoryLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChangeTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompetitorChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ChangeTimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitorChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetitorChanges_ChangeTypes_ChangeTypeId",
                        column: x => x.ChangeTypeId,
                        principalTable: "ChangeTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompetitorChanges_Competitors_CompetitorId",
                        column: x => x.CompetitorId,
                        principalTable: "Competitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompetitorChanges_ChangeTypeId",
                table: "CompetitorChanges",
                column: "ChangeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitorChanges_CompetitorId",
                table: "CompetitorChanges",
                column: "CompetitorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompetitorChanges");

            migrationBuilder.DropTable(
                name: "ChangeTypes");
        }
    }
}
