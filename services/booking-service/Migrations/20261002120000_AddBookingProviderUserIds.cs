using BookingService.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BookingService.Migrations;

[DbContext(typeof(BookingDbContext))]
[Migration("20261002120000_AddBookingProviderUserIds")]
public class AddBookingProviderUserIds : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var table in new[] { "Bookings", "RestaurantReservations", "AccommodationBookings" })
            migrationBuilder.AddColumn<Guid>("ProviderUserId", table, type: "char(36)", nullable: true,
                collation: "ascii_general_ci");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in new[] { "Bookings", "RestaurantReservations", "AccommodationBookings" })
            migrationBuilder.DropColumn("ProviderUserId", table);
    }
}
