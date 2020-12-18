using Microsoft.EntityFrameworkCore.Migrations;

namespace SailScores.Web.Data.Migrations
{
    public partial class AddAppInsightsSetting : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableAppInsights",
                table: "AspNetUsers",
                type: "bit",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableAppInsights",
                table: "AspNetUsers");
        }
    }
}
