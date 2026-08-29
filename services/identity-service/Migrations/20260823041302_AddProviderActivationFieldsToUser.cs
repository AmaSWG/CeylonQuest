using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityService.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderActivationFieldsToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Users columns ─────────────────────────────────────────────────────────
            // Each column is added via a stored procedure that first checks
            // INFORMATION_SCHEMA, making the migration safe to run on a DB where some
            // or all columns already exist (e.g. added manually or via a previous
            // out-of-band SQL script).  Works with MySQL 5.7+ / Azure Database for MySQL.

            migrationBuilder.Sql(@"
DROP PROCEDURE IF EXISTS _mig_add_col_if_missing;
CREATE PROCEDURE _mig_add_col_if_missing()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME   = 'Users'
          AND COLUMN_NAME  = 'IsActive'
    ) THEN
        ALTER TABLE `Users` ADD COLUMN `IsActive` tinyint(1) NOT NULL DEFAULT 1;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME   = 'Users'
          AND COLUMN_NAME  = 'OtpCode'
    ) THEN
        ALTER TABLE `Users` ADD COLUMN `OtpCode` longtext CHARACTER SET utf8mb4 NULL;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME   = 'Users'
          AND COLUMN_NAME  = 'OtpExpiresAt'
    ) THEN
        ALTER TABLE `Users` ADD COLUMN `OtpExpiresAt` datetime(6) NULL;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME   = 'Users'
          AND COLUMN_NAME  = 'RequiresPasswordChange'
    ) THEN
        ALTER TABLE `Users` ADD COLUMN `RequiresPasswordChange` tinyint(1) NOT NULL DEFAULT 0;
    END IF;
END;
CALL _mig_add_col_if_missing();
DROP PROCEDURE IF EXISTS _mig_add_col_if_missing;
");

            // ── ProviderApplications table ────────────────────────────────────────────
            // The table was created manually (via raw SQL in Program.cs startup fallback
            // or the AddProviderApplications migration that was never recorded in
            // __EFMigrationsHistory).  CREATE TABLE IF NOT EXISTS is universally supported.
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS `ProviderApplications` (
    `Id`                    char(36)    NOT NULL,
    `FirstName`             longtext    CHARACTER SET utf8mb4 NOT NULL,
    `LastName`              longtext    CHARACTER SET utf8mb4 NOT NULL,
    `Email`                 longtext    CHARACTER SET utf8mb4 NOT NULL,
    `PhoneNumber`           longtext    CHARACTER SET utf8mb4 NOT NULL,
    `BusinessName`          longtext    CHARACTER SET utf8mb4 NOT NULL,
    `ServiceType`           longtext    CHARACTER SET utf8mb4 NOT NULL,
    `Location`              longtext    CHARACTER SET utf8mb4 NOT NULL,
    `Description`           longtext    CHARACTER SET utf8mb4 NOT NULL,
    `LegalDocumentFileName` longtext    CHARACTER SET utf8mb4 NULL,
    `Status`                int         NOT NULL,
    `CreatedAt`             datetime(6) NOT NULL,
    PRIMARY KEY (`Id`)
) CHARACTER SET = utf8mb4;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProviderApplications");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OtpCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OtpExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RequiresPasswordChange",
                table: "Users");
        }
    }
}
