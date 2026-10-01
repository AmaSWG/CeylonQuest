using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProviderCatalogService.Migrations
{
    /// <inheritdoc />
    public partial class AddImagesToListings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Images",
                table: "RestaurantListings",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Images",
                table: "ActivityListings",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Images",
                table: "AccommodationListings",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Images",
                table: "RestaurantListings");

            migrationBuilder.DropColumn(
                name: "Images",
                table: "ActivityListings");

            migrationBuilder.DropColumn(
                name: "Images",
                table: "AccommodationListings");
        }
    }
}
