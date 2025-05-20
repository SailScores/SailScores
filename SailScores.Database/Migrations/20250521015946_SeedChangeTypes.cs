using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class SeedChangeTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ChangeTypes",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { new Guid("153a8b2a-accf-404c-bb39-61db55f5ee1e"), null, "Activated" },
                    { new Guid("87533c82-936d-44bb-8055-9292046a7b9e"), null, "Deactivated" },
                    { new Guid("b6c92ed8-1d15-4a1a-977f-6e59bd0160c7"), null, "Created" },
                    { new Guid("ee49c9c4-d556-4cab-b740-a3baad9c73c9"), null, "Deleted" },
                    { new Guid("f2a0b1d4-3c5e-4f8b-9a7c-6d8e5f2b0c3d"), null, "Property Changed" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ChangeTypes",
                keyColumn: "Id",
                keyValue: new Guid("153a8b2a-accf-404c-bb39-61db55f5ee1e"));

            migrationBuilder.DeleteData(
                table: "ChangeTypes",
                keyColumn: "Id",
                keyValue: new Guid("87533c82-936d-44bb-8055-9292046a7b9e"));

            migrationBuilder.DeleteData(
                table: "ChangeTypes",
                keyColumn: "Id",
                keyValue: new Guid("b6c92ed8-1d15-4a1a-977f-6e59bd0160c7"));

            migrationBuilder.DeleteData(
                table: "ChangeTypes",
                keyColumn: "Id",
                keyValue: new Guid("ee49c9c4-d556-4cab-b740-a3baad9c73c9"));

            migrationBuilder.DeleteData(
                table: "ChangeTypes",
                keyColumn: "Id",
                keyValue: new Guid("f2a0b1d4-3c5e-4f8b-9a7c-6d8e5f2b0c3d"));
        }
    }
}
