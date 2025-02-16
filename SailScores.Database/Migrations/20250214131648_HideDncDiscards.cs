using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class HideDncDiscards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HideDncDiscards",
                table: "Series",
                type: "bit",
                nullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropColumn(
                name: "HideDncDiscards",
                table: "Series");

        }
    }
}
