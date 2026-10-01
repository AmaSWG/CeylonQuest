CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `BookingContexts` (
    `BookingId` char(36) COLLATE ascii_general_ci NOT NULL,
    `VisitorId` char(36) COLLATE ascii_general_ci NOT NULL,
    `ProviderUserId` char(36) COLLATE ascii_general_ci NOT NULL,
    CONSTRAINT `PK_BookingContexts` PRIMARY KEY (`BookingId`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `FailedEvents` (
    `EventKey` varchar(160) CHARACTER SET utf8mb4 NOT NULL,
    `Topic` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `Payload` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Error` longtext CHARACTER SET utf8mb4 NOT NULL,
    `AttemptCount` int NOT NULL,
    `LastAttemptAtUtc` datetime(6) NOT NULL,
    `Resolved` tinyint(1) NOT NULL,
    CONSTRAINT `PK_FailedEvents` PRIMARY KEY (`EventKey`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Notifications` (
    `Id` char(36) COLLATE ascii_general_ci NOT NULL,
    `RecipientUserId` char(36) COLLATE ascii_general_ci NOT NULL,
    `EventKey` varchar(160) CHARACTER SET utf8mb4 NOT NULL,
    `EventType` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `Title` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Message` longtext CHARACTER SET utf8mb4 NOT NULL,
    `BookingId` char(36) COLLATE ascii_general_ci NOT NULL,
    `ListingId` char(36) COLLATE ascii_general_ci NULL,
    `PaymentId` char(36) COLLATE ascii_general_ci NULL,
    `ReviewId` char(36) COLLATE ascii_general_ci NULL,
    `Amount` decimal(18,2) NULL,
    `Currency` varchar(10) CHARACTER SET utf8mb4 NULL,
    `Rating` int NULL,
    `RefundStatus` varchar(64) CHARACTER SET utf8mb4 NULL,
    `RefundAmount` decimal(18,2) NULL,
    `CreatedAtUtc` datetime(6) NOT NULL,
    `OccurredAtUtc` datetime(6) NOT NULL,
    `IsRead` tinyint(1) NOT NULL,
    `ReadAtUtc` datetime(6) NULL,
    CONSTRAINT `PK_Notifications` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `ProcessedEvents` (
    `EventKey` varchar(160) CHARACTER SET utf8mb4 NOT NULL,
    `Topic` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `ProcessedAtUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_ProcessedEvents` PRIMARY KEY (`EventKey`)
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_Notifications_EventKey_RecipientUserId` ON `Notifications` (`EventKey`, `RecipientUserId`);

CREATE INDEX `IX_Notifications_RecipientUserId_IsRead_CreatedAtUtc` ON `Notifications` (`RecipientUserId`, `IsRead`, `CreatedAtUtc`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261001160230_InitialNotifications', '8.0.30');

COMMIT;

