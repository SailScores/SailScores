using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SailScores.Database.Migrations
{
    public partial class ScoringSystemHierarchy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScoreCodes_Clubs_ClubId",
                table: "ScoreCodes");

            migrationBuilder.DropForeignKey(
                name: "FK_ScoreCodes_ScoringSystems_ScoringSystemId",
                table: "ScoreCodes");

            migrationBuilder.AlterColumn<Guid>(
                name: "ClubId",
                table: "ScoringSystems",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.AddColumn<Guid>(
                name: "ParentSystemId",
                table: "ScoringSystems",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ScoringSystemId",
                table: "ScoreCodes",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ClubId",
                table: "ScoreCodes",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.CreateIndex(
                name: "IX_ScoringSystems_ParentSystemId",
                table: "ScoringSystems",
                column: "ParentSystemId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScoreCodes_Clubs_ClubId",
                table: "ScoreCodes",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScoreCodes_ScoringSystems_ScoringSystemId",
                table: "ScoreCodes",
                column: "ScoringSystemId",
                principalTable: "ScoringSystems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ScoringSystems_ScoringSystems_ParentSystemId",
                table: "ScoringSystems",
                column: "ParentSystemId",
                principalTable: "ScoringSystems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScoreCodes_Clubs_ClubId",
                table: "ScoreCodes");

            migrationBuilder.DropForeignKey(
                name: "FK_ScoreCodes_ScoringSystems_ScoringSystemId",
                table: "ScoreCodes");

            migrationBuilder.DropForeignKey(
                name: "FK_ScoringSystems_ScoringSystems_ParentSystemId",
                table: "ScoringSystems");

            migrationBuilder.DropIndex(
                name: "IX_ScoringSystems_ParentSystemId",
                table: "ScoringSystems");

            migrationBuilder.DropColumn(
                name: "ParentSystemId",
                table: "ScoringSystems");

            migrationBuilder.AlterColumn<Guid>(
                name: "ClubId",
                table: "ScoringSystems",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ScoringSystemId",
                table: "ScoreCodes",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.AlterColumn<Guid>(
                name: "ClubId",
                table: "ScoreCodes",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ScoreCodes_Clubs_ClubId",
                table: "ScoreCodes",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ScoreCodes_ScoringSystems_ScoringSystemId",
                table: "ScoreCodes",
                column: "ScoringSystemId",
                principalTable: "ScoringSystems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
