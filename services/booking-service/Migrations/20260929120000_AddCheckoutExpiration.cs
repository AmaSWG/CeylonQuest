using BookingService.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BookingService.Migrations;

[DbContext(typeof(BookingDbContext))]
[Migration("20260929120000_AddCheckoutExpiration")]
public class AddCheckoutExpiration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("CheckoutSessionId", "PaymentTransaction", type: "varchar(255)", maxLength: 255, nullable: true);
        migrationBuilder.AddColumn<DateTime>("CheckoutDeadline", "PaymentTransaction", type: "datetime(6)", nullable: true);
        migrationBuilder.AddColumn<DateTime>("CheckoutClosedAt", "PaymentTransaction", type: "datetime(6)", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("CheckoutSessionId", "PaymentTransaction");
        migrationBuilder.DropColumn("CheckoutDeadline", "PaymentTransaction");
        migrationBuilder.DropColumn("CheckoutClosedAt", "PaymentTransaction");
    }
}
