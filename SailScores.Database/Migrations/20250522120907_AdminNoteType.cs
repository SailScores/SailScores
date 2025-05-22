using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class AdminNoteType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ChangeTypes",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[] { new Guid("9b1af84e-8179-4345-9583-2bf741b111bd"), null, "Admin Note" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ChangeTypes",
                keyColumn: "Id",
                keyValue: new Guid("9b1af84e-8179-4345-9583-2bf741b111bd"));
        }
    }
}
