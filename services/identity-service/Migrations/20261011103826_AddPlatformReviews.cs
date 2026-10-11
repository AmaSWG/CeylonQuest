using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityService.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlatformReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    UserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformReviews", x => x.Id);
                    table.CheckConstraint("CK_PlatformReviews_Rating", "`Rating` BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_PlatformReviews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformReviews_CreatedAtUtc",
                table: "PlatformReviews",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformReviews_Rating_CreatedAtUtc",
                table: "PlatformReviews",
                columns: new[] { "Rating", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformReviews_UserId",
                table: "PlatformReviews",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlatformReviews");
        }
    }
}
