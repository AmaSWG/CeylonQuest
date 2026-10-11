using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace booking_service.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceVerificationAndReviewResponses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Topic",
                table: "ReviewOutboxMessages",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "review.submitted")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AttendanceStatus",
                table: "RestaurantReservations",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "AttendanceVerifiedAtUtc",
                table: "RestaurantReservations",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AttendanceVerifiedByUserId",
                table: "RestaurantReservations",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<bool>(
                name: "AttendanceVerified",
                table: "ListingReviews",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProviderRespondedAtUtc",
                table: "ListingReviews",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProviderRespondedByUserId",
                table: "ListingReviews",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "ProviderResponse",
                table: "ListingReviews",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AttendanceStatus",
                table: "Bookings",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "AttendanceVerifiedAtUtc",
                table: "Bookings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AttendanceVerifiedByUserId",
                table: "Bookings",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "AttendanceStatus",
                table: "AccommodationBookings",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "AttendanceVerifiedAtUtc",
                table: "AccommodationBookings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AttendanceVerifiedByUserId",
                table: "AccommodationBookings",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "AttendanceDisputes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    BookingId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    BookingType = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VisitorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    OriginalAttendanceStatus = table.Column<int>(type: "int", nullable: false),
                    OriginalVerifiedByUserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    OriginalVerifiedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Reason = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Resolution = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResolvedByUserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ResolvedAttendanceStatus = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceDisputes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceDisputes_BookingType_BookingId",
                table: "AttendanceDisputes",
                columns: new[] { "BookingType", "BookingId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceDisputes");

            migrationBuilder.DropColumn(
                name: "Topic",
                table: "ReviewOutboxMessages");

            migrationBuilder.DropColumn(
                name: "AttendanceStatus",
                table: "RestaurantReservations");

            migrationBuilder.DropColumn(
                name: "AttendanceVerifiedAtUtc",
                table: "RestaurantReservations");

            migrationBuilder.DropColumn(
                name: "AttendanceVerifiedByUserId",
                table: "RestaurantReservations");

            migrationBuilder.DropColumn(
                name: "AttendanceVerified",
                table: "ListingReviews");

            migrationBuilder.DropColumn(
                name: "ProviderRespondedAtUtc",
                table: "ListingReviews");

            migrationBuilder.DropColumn(
                name: "ProviderRespondedByUserId",
                table: "ListingReviews");

            migrationBuilder.DropColumn(
                name: "ProviderResponse",
                table: "ListingReviews");

            migrationBuilder.DropColumn(
                name: "AttendanceStatus",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "AttendanceVerifiedAtUtc",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "AttendanceVerifiedByUserId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "AttendanceStatus",
                table: "AccommodationBookings");

            migrationBuilder.DropColumn(
                name: "AttendanceVerifiedAtUtc",
                table: "AccommodationBookings");

            migrationBuilder.DropColumn(
                name: "AttendanceVerifiedByUserId",
                table: "AccommodationBookings");
        }
    }
}
