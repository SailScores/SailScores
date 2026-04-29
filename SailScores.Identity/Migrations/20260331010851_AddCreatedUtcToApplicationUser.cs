using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedUtcToApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedUtc",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedUtc",
                table: "AspNetUsers");
        }
    }
}
