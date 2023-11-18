using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    public partial class CodeParticipation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_RegattaForwarders_Series_NewRegattaId",
            //    table: "RegattaForwarders");

            migrationBuilder.AddColumn<bool>(
                name: "CountAsParticipation",
                table: "ScoreCodes",
                type: "bit",
                nullable: true);

            //migrationBuilder.AddForeignKey(
            //    name: "FK_RegattaForwarders_Regattas_NewRegattaId",
            //    table: "RegattaForwarders",
            //    column: "NewRegattaId",
            //    principalTable: "Regattas",
            //    principalColumn: "Id",
            //    onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_RegattaForwarders_Regattas_NewRegattaId",
            //    table: "RegattaForwarders");

            migrationBuilder.DropColumn(
                name: "CountAsParticipation",
                table: "ScoreCodes");

            //migrationBuilder.AddForeignKey(
            //    name: "FK_RegattaForwarders_Series_NewRegattaId",
            //    table: "RegattaForwarders",
            //    column: "NewRegattaId",
            //    principalTable: "Series",
            //    principalColumn: "Id",
            //    onDelete: ReferentialAction.Cascade);
        }
    }
}
