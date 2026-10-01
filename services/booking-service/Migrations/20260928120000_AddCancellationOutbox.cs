using BookingService.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BookingService.Migrations;

[DbContext(typeof(BookingDbContext))]
[Migration("20260928120000_AddCancellationOutbox")]
public class AddCancellationOutbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.CreateTable(
            name: "BookingCancellationMessages",
            columns: table => new
            {
                BookingId = table.Column<Guid>(type: "char(36)", nullable: false),
                Payload = table.Column<string>(type: "longtext", nullable: false),
                PublishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_BookingCancellationMessages", x => x.BookingId));

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable("BookingCancellationMessages");
}
