using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class SeriesOfSeries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ChildrenSeriesAsSingleRace",
                table: "Series",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Series",
                type: "int",
                nullable: true,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "SeriesToSeriesLink",
                columns: table => new
                {
                    ParentSeriesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChildSeriesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesToSeriesLink", x => new { x.ParentSeriesId, x.ChildSeriesId });
                    table.ForeignKey(
                        name: "FK_SeriesToSeriesLink_Series_ChildSeriesId",
                        column: x => x.ChildSeriesId,
                        principalTable: "Series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SeriesToSeriesLink_Series_ParentSeriesId",
                        column: x => x.ParentSeriesId,
                        principalTable: "Series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeriesToSeriesLink_ChildSeriesId",
                table: "SeriesToSeriesLink",
                column: "ChildSeriesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeriesToSeriesLink");

            migrationBuilder.DropColumn(
                name: "ChildrenSeriesAsSingleRace",
                table: "Series");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Series");
        }
    }
}
