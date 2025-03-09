using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class CompUrlName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UrlId",
                table: "Competitors",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UrlName",
                table: "Competitors",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClubSequences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClubId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NextValue = table.Column<int>(type: "int", nullable: false),
                    SequenceType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SequencePrefix = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SequenceSuffix = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubSequences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClubSequences_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClubSequences_ClubId",
                table: "ClubSequences",
                column: "ClubId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClubSequences");

            migrationBuilder.DropColumn(
                name: "UrlId",
                table: "Competitors");

            migrationBuilder.DropColumn(
                name: "UrlName",
                table: "Competitors");
        }
    }
}
