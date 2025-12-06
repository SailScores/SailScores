using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddClubSubscriptionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AdvancedFeaturesEnabledDate",
                table: "Clubs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionType",
                table: "Clubs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdvancedFeaturesEnabledDate",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "SubscriptionType",
                table: "Clubs");
        }
    }
}
