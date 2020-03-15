using Microsoft.EntityFrameworkCore.Migrations;

namespace SailScores.Database.Migrations
{
    public partial class BoatClassDeleteFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FleetBoatClass_Fleets_FleetId",
                table: "FleetBoatClass");

            migrationBuilder.AddForeignKey(
                name: "FK_FleetBoatClass_Fleets_FleetId",
                table: "FleetBoatClass",
                column: "FleetId",
                principalTable: "Fleets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FleetBoatClass_Fleets_FleetId",
                table: "FleetBoatClass");

            migrationBuilder.AddForeignKey(
                name: "FK_FleetBoatClass_Fleets_FleetId",
                table: "FleetBoatClass",
                column: "FleetId",
                principalTable: "Fleets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
