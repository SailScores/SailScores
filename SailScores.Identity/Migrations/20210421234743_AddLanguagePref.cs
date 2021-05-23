using Microsoft.EntityFrameworkCore.Migrations;

namespace SailScores.Web.Data.Migrations
{
    public partial class AddLanguagePref : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SpeechRecognitionLanguage",
                table: "AspNetUsers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpeechRecognitionLanguage",
                table: "AspNetUsers");
        }
    }
}
