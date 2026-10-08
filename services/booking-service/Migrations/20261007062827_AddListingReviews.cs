using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace booking_service.Migrations
{
    /// <inheritdoc />
    public partial class AddListingReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProviderId",
                table: "RestaurantReservations",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledEndAtUtc",
                table: "RestaurantReservations",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProviderId",
                table: "Bookings",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledEndAtUtc",
                table: "Bookings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProviderId",
                table: "AccommodationBookings",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledEndAtUtc",
                table: "AccommodationBookings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ListingReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    BookingId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    BookingType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VisitorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ListingId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ProviderId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingReviews", x => x.Id);
                    table.CheckConstraint("CK_ListingReviews_Rating", "`Rating` BETWEEN 1 AND 5");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReviewOutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Payload = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewOutboxMessages", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ListingReviews_BookingType_BookingId",
                table: "ListingReviews",
                columns: new[] { "BookingType", "BookingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ListingReviews_ListingId_BookingType_Rating_CreatedAtUtc",
                table: "ListingReviews",
                columns: new[] { "ListingId", "BookingType", "Rating", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewOutboxMessages_PublishedAtUtc",
                table: "ReviewOutboxMessages",
                column: "PublishedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListingReviews");

            migrationBuilder.DropTable(
                name: "ReviewOutboxMessages");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                table: "RestaurantReservations");

            migrationBuilder.DropColumn(
                name: "ScheduledEndAtUtc",
                table: "RestaurantReservations");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ScheduledEndAtUtc",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                table: "AccommodationBookings");

            migrationBuilder.DropColumn(
                name: "ScheduledEndAtUtc",
                table: "AccommodationBookings");
        }
    }
}
