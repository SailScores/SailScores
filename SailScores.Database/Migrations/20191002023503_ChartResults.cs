using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SailScores.Database.Migrations
{
    public partial class ChartResults : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SeriesChartResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SeriesId = table.Column<Guid>(nullable: false),
                    IsCurrent = table.Column<bool>(nullable: false),
                    Results = table.Column<string>(nullable: true),
                    Created = table.Column<DateTime>(nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesChartResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeriesChartResults_Series_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "Series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeriesChartResults_SeriesId",
                table: "SeriesChartResults",
                column: "SeriesId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeriesChartResults");
        }
    }
}
