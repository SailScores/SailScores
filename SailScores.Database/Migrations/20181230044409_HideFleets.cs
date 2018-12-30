using Microsoft.EntityFrameworkCore.Migrations;

namespace Sailscores.Database.Migrations
{
    public partial class HideFleets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "Fleets",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "Fleets");
        }
    }
}
