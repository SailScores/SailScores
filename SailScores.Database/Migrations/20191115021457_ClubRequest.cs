using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SailScores.Database.Migrations
{
    public partial class ClubRequest : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "ParticipationPercent",
                table: "ScoringSystems",
                type: "decimal(18, 2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CodePoints",
                table: "Scores",
                type: "decimal(18, 2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ClubRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ClubName = table.Column<string>(maxLength: 200, nullable: false),
                    ClubInitials = table.Column<string>(maxLength: 10, nullable: true),
                    ClubLocation = table.Column<string>(nullable: true),
                    ClubWebsite = table.Column<string>(nullable: true),
                    ContactName = table.Column<string>(nullable: true),
                    ContactEmail = table.Column<string>(nullable: true),
                    Hide = table.Column<bool>(nullable: true),
                    ForTesting = table.Column<bool>(nullable: true),
                    Classes = table.Column<string>(nullable: true),
                    TypicalDiscardRules = table.Column<string>(nullable: true),
                    Comments = table.Column<string>(nullable: true),
                    RequestSubmitted = table.Column<DateTime>(nullable: true),
                    RequestApproved = table.Column<DateTime>(nullable: true),
                    AdminNotes = table.Column<string>(nullable: true),
                    TestClubId = table.Column<Guid>(nullable: true),
                    VisibleClubId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubRequests", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClubRequests");

            migrationBuilder.AlterColumn<decimal>(
                name: "ParticipationPercent",
                table: "ScoringSystems",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18, 2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CodePoints",
                table: "Scores",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18, 2)",
                oldNullable: true);
        }
    }
}
