using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProviderCatalogService.Data;

namespace ProviderCatalogService.Migrations;

[DbContext(typeof(CatalogDbContext))]
[Migration("20260928120000_AddBookingCapacityReleases")]
public class AddBookingCapacityReleases : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.CreateTable(
            name: "BookingCapacityReleases",
            columns: table => new
            {
                BookingId = table.Column<Guid>(type: "char(36)", nullable: false),
                ReleasedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_BookingCapacityReleases", x => x.BookingId));

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable("BookingCapacityReleases");
}
