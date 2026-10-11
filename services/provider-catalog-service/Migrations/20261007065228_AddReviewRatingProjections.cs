using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProviderCatalogService.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewRatingProjections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AverageRating",
                table: "RestaurantListings",
                type: "decimal(3,2)",
                precision: 3,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "RatingSum",
                table: "RestaurantListings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "ReviewCount",
                table: "RestaurantListings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "AverageRating",
                table: "ActivityListings",
                type: "decimal(3,2)",
                precision: 3,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "RatingSum",
                table: "ActivityListings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "ReviewCount",
                table: "ActivityListings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "AverageRating",
                table: "AccommodationListings",
                type: "decimal(3,2)",
                precision: 3,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "RatingSum",
                table: "AccommodationListings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "ReviewCount",
                table: "AccommodationListings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ReviewRatingContributions",
                columns: table => new
                {
                    ReviewId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    EventId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ListingId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    BookingType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewRatingContributions", x => x.ReviewId);
                    table.CheckConstraint("CK_ReviewRatingContributions_Rating", "`Rating` BETWEEN 1 AND 5");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewRatingContributions_EventId",
                table: "ReviewRatingContributions",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewRatingContributions_ListingId_BookingType",
                table: "ReviewRatingContributions",
                columns: new[] { "ListingId", "BookingType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReviewRatingContributions");

            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "RestaurantListings");

            migrationBuilder.DropColumn(
                name: "RatingSum",
                table: "RestaurantListings");

            migrationBuilder.DropColumn(
                name: "ReviewCount",
                table: "RestaurantListings");

            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "ActivityListings");

            migrationBuilder.DropColumn(
                name: "RatingSum",
                table: "ActivityListings");

            migrationBuilder.DropColumn(
                name: "ReviewCount",
                table: "ActivityListings");

            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "AccommodationListings");

            migrationBuilder.DropColumn(
                name: "RatingSum",
                table: "AccommodationListings");

            migrationBuilder.DropColumn(
                name: "ReviewCount",
                table: "AccommodationListings");
        }
    }
}
