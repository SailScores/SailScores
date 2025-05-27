using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class MergeChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ChangeTypes",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[] { new Guid("1c28a4cb-5994-44ec-9b2b-aceb3036256b"), null, "Merged" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ChangeTypes",
                keyColumn: "Id",
                keyValue: new Guid("1c28a4cb-5994-44ec-9b2b-aceb3036256b"));
        }
    }
}
