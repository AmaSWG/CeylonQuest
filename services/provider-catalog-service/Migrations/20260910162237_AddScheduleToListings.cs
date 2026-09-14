using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProviderCatalogService.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleToListings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvailableDays",
                table: "ActivityListings",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Duration",
                table: "ActivityListings",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TimeSlots",
                table: "ActivityListings",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidFrom",
                table: "ActivityListings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidUntil",
                table: "ActivityListings",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailableDays",
                table: "ActivityListings");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "ActivityListings");

            migrationBuilder.DropColumn(
                name: "TimeSlots",
                table: "ActivityListings");

            migrationBuilder.DropColumn(
                name: "ValidFrom",
                table: "ActivityListings");

            migrationBuilder.DropColumn(
                name: "ValidUntil",
                table: "ActivityListings");
        }
    }
}
