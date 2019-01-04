using Microsoft.EntityFrameworkCore.Migrations;

namespace SailScores.Database.Migrations
{
    public partial class BoatClassRename : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoatClass_Clubs_ClubId",
                table: "BoatClass");

            migrationBuilder.DropForeignKey(
                name: "FK_Competitors_BoatClass_BoatClassId",
                table: "Competitors");

            migrationBuilder.DropForeignKey(
                name: "FK_FleetBoatClass_BoatClass_BoatClassId",
                table: "FleetBoatClass");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BoatClass",
                table: "BoatClass");

            migrationBuilder.RenameTable(
                name: "BoatClass",
                newName: "BoatClasses");

            migrationBuilder.RenameIndex(
                name: "IX_BoatClass_ClubId",
                table: "BoatClasses",
                newName: "IX_BoatClasses_ClubId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BoatClasses",
                table: "BoatClasses",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BoatClasses_Clubs_ClubId",
                table: "BoatClasses",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Competitors_BoatClasses_BoatClassId",
                table: "Competitors",
                column: "BoatClassId",
                principalTable: "BoatClasses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FleetBoatClass_BoatClasses_BoatClassId",
                table: "FleetBoatClass",
                column: "BoatClassId",
                principalTable: "BoatClasses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoatClasses_Clubs_ClubId",
                table: "BoatClasses");

            migrationBuilder.DropForeignKey(
                name: "FK_Competitors_BoatClasses_BoatClassId",
                table: "Competitors");

            migrationBuilder.DropForeignKey(
                name: "FK_FleetBoatClass_BoatClasses_BoatClassId",
                table: "FleetBoatClass");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BoatClasses",
                table: "BoatClasses");

            migrationBuilder.RenameTable(
                name: "BoatClasses",
                newName: "BoatClass");

            migrationBuilder.RenameIndex(
                name: "IX_BoatClasses_ClubId",
                table: "BoatClass",
                newName: "IX_BoatClass_ClubId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BoatClass",
                table: "BoatClass",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BoatClass_Clubs_ClubId",
                table: "BoatClass",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Competitors_BoatClass_BoatClassId",
                table: "Competitors",
                column: "BoatClassId",
                principalTable: "BoatClass",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FleetBoatClass_BoatClass_BoatClassId",
                table: "FleetBoatClass",
                column: "BoatClassId",
                principalTable: "BoatClass",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
