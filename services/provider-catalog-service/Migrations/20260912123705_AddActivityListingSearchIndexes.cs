using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProviderCatalogService.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityListingSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ActivityListings_IsActive_CreatedAt",
                table: "ActivityListings",
                columns: new[] { "IsActive", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityListings_IsActive_Price",
                table: "ActivityListings",
                columns: new[] { "IsActive", "Price" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ActivityListings_IsActive_CreatedAt",
                table: "ActivityListings");

            migrationBuilder.DropIndex(
                name: "IX_ActivityListings_IsActive_Price",
                table: "ActivityListings");
        }
    }
}
