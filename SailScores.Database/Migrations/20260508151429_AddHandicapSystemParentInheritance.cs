using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddHandicapSystemParentInheritance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentSystemId",
                table: "HandicapSystems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "HandicapSystems",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-0001-0000-0000-000000000001"),
                column: "ParentSystemId",
                value: null);

            migrationBuilder.UpdateData(
                table: "HandicapSystems",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-0001-0000-0000-000000000002"),
                column: "ParentSystemId",
                value: null);

            migrationBuilder.UpdateData(
                table: "HandicapSystems",
                keyColumn: "Id",
                keyValue: new Guid("a1b2c3d4-0001-0000-0000-000000000003"),
                column: "ParentSystemId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_HandicapSystems_ParentSystemId",
                table: "HandicapSystems",
                column: "ParentSystemId");

            migrationBuilder.AddForeignKey(
                name: "FK_HandicapSystems_HandicapSystems_ParentSystemId",
                table: "HandicapSystems",
                column: "ParentSystemId",
                principalTable: "HandicapSystems",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HandicapSystems_HandicapSystems_ParentSystemId",
                table: "HandicapSystems");

            migrationBuilder.DropIndex(
                name: "IX_HandicapSystems_ParentSystemId",
                table: "HandicapSystems");

            migrationBuilder.DropColumn(
                name: "ParentSystemId",
                table: "HandicapSystems");
        }
    }
}
