using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SailScores.Database.Migrations
{
    public partial class SiteDeafaultScoring : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSiteDefault",
                table: "ScoringSystems",
                type: "bit",
                nullable: true);


        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSiteDefault",
                table: "ScoringSystems");


        }
    }
}
