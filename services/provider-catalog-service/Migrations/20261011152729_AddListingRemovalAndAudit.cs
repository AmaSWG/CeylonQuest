using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProviderCatalogService.Migrations
{
    /// <inheritdoc />
    public partial class AddListingRemovalAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRemoved",
                table: "RestaurantListings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRemoved",
                table: "ActivityListings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRemoved",
                table: "AccommodationListings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ListingRemovalAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ListingId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ListingType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AdministratorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RemovedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PreviousStatus = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NewStatus = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingRemovalAudits", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantListings_IsRemoved_IsActive",
                table: "RestaurantListings",
                columns: new[] { "IsRemoved", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityListings_IsRemoved_IsActive",
                table: "ActivityListings",
                columns: new[] { "IsRemoved", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationListings_IsRemoved_IsActive",
                table: "AccommodationListings",
                columns: new[] { "IsRemoved", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ListingRemovalAudits_ListingType_ListingId",
                table: "ListingRemovalAudits",
                columns: new[] { "ListingType", "ListingId" });

            migrationBuilder.CreateIndex(
                name: "IX_ListingRemovalAudits_RemovedAtUtc",
                table: "ListingRemovalAudits",
                column: "RemovedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListingRemovalAudits");

            migrationBuilder.DropIndex(
                name: "IX_RestaurantListings_IsRemoved_IsActive",
                table: "RestaurantListings");

            migrationBuilder.DropIndex(
                name: "IX_ActivityListings_IsRemoved_IsActive",
                table: "ActivityListings");

            migrationBuilder.DropIndex(
                name: "IX_AccommodationListings_IsRemoved_IsActive",
                table: "AccommodationListings");

            migrationBuilder.DropColumn(
                name: "IsRemoved",
                table: "RestaurantListings");

            migrationBuilder.DropColumn(
                name: "IsRemoved",
                table: "ActivityListings");

            migrationBuilder.DropColumn(
                name: "IsRemoved",
                table: "AccommodationListings");
        }
    }
}
