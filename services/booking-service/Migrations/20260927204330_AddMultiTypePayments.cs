using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace booking_service.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTypePayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentTransaction_Bookings_BookingId",
                table: "PaymentTransaction");

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "RestaurantReservations",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "RestaurantReservations",
                type: "longtext",
                nullable: false,
                defaultValue: "Unpaid")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BookingType",
                table: "PaymentTransaction",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Experience")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "AccommodationBookings",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "AccommodationBookings",
                type: "longtext",
                nullable: false,
                defaultValue: "Unpaid")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "RestaurantReservations");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "RestaurantReservations");

            migrationBuilder.DropColumn(
                name: "BookingType",
                table: "PaymentTransaction");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "AccommodationBookings");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "AccommodationBookings");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentTransaction_Bookings_BookingId",
                table: "PaymentTransaction",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
