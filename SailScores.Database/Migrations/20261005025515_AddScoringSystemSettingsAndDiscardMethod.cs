using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddScoringSystemSettingsAndDiscardMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiscardMethod",
                table: "ScoringSystems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Settings",
                table: "ScoringSystems",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscardMethod",
                table: "ScoringSystems");

            migrationBuilder.DropColumn(
                name: "Settings",
                table: "ScoringSystems");
        }
    }
}
