using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLastSuccessfulLoginUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSuccessfulLoginUtc",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSuccessfulLoginUtc",
                table: "AspNetUsers");
        }
    }
}
