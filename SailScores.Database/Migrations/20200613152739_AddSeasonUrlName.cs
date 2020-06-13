using Microsoft.EntityFrameworkCore.Migrations;

namespace SailScores.Database.Migrations
{
    public partial class AddSeasonUrlName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UrlName",
                table: "Seasons",
                maxLength: 200,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UrlName",
                table: "Seasons");
        }
    }
}
