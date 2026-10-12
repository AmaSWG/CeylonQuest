using System;
using System.Threading;
using System.Threading.Tasks;
using CeylonQuest.Tests.Sprint4Notifications;
using MySqlConnector;

namespace CeylonQuest.Tests.Sprint4Reviews.Infrastructure;

/// <summary>
/// Direct database helper for Sprint 4 QA tests to mark a test booking as 'Completed'
/// in the past, enabling end-to-end review submission without needing manual provider OTP approval.
/// </summary>
public static class BookingDbHelper
{
    public static async Task MarkBookingCompletedAsync(Guid bookingId, Guid? providerUserId = null, CancellationToken ct = default)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                await using var connection = new MySqlConnection(TestConfig.BookingDbConnectionString);
                await connection.OpenAsync(ct);

                var sql = @"
                    UPDATE Bookings
                    SET Status = 'Completed',
                        PaymentStatus = 'Paid',
                        AttendanceStatus = 'Attended',
                        BookingDate = @PastDate,
                        TimeSlot = '09:00 - 11:00',
                        ScheduledEndAtUtc = @PastEnd,
                        AttendanceVerifiedAtUtc = @Now,
                        ProviderId = COALESCE(ProviderId, 'e217c77b-d400-48b2-8a8d-b5a56da975a0'),
                        UpdatedAt = @Now" +
                        (providerUserId.HasValue ? ", ProviderUserId = @ProviderUserId" : "") +
                    " WHERE Id = @BookingId";

                await using var command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@BookingId", bookingId.ToString());
                command.Parameters.AddWithValue("@PastDate", DateTime.UtcNow.AddDays(-2).ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@PastEnd", DateTime.UtcNow.AddDays(-2));
                command.Parameters.AddWithValue("@Now", DateTime.UtcNow);
                if (providerUserId.HasValue)
                    command.Parameters.AddWithValue("@ProviderUserId", providerUserId.Value.ToString());

                await command.ExecuteNonQueryAsync(ct);
                return;
            }
            catch (MySqlException) when (attempt < 2)
            {
                await Task.Delay(1000, ct);
            }
        }
    }

    public static async Task SetBookingStatusAsync(Guid bookingId, string status, string paymentStatus = "Unpaid", CancellationToken ct = default)
    {
        await using var connection = new MySqlConnection(TestConfig.BookingDbConnectionString);
        await connection.OpenAsync(ct);

        var sql = @"
            UPDATE Bookings
            SET Status = @Status,
                PaymentStatus = @PaymentStatus,
                UpdatedAt = @Now
            WHERE Id = @BookingId";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@BookingId", bookingId.ToString());
        command.Parameters.AddWithValue("@Status", status);
        command.Parameters.AddWithValue("@PaymentStatus", paymentStatus);
        command.Parameters.AddWithValue("@Now", DateTime.UtcNow);

        await command.ExecuteNonQueryAsync(ct);
    }

    public static async Task ClearReviewsForListingAsync(Guid listingId, CancellationToken ct = default)
    {
        await using var connection = new MySqlConnection(TestConfig.BookingDbConnectionString);
        await connection.OpenAsync(ct);

        const string sql = "DELETE FROM ListingReviews WHERE ListingId = @ListingId";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ListingId", listingId.ToString());
        await command.ExecuteNonQueryAsync(ct);
    }

    public static async Task<string> GetLatestOutboxAsync(CancellationToken ct = default)
    {
        await using var connection = new MySqlConnection(TestConfig.BookingDbConnectionString);
        await connection.OpenAsync(ct);
        const string sql = "SELECT Topic, PublishedAtUtc, Payload FROM ReviewOutboxMessages ORDER BY PublishedAtUtc DESC LIMIT 3";
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var res = "";
        while (await reader.ReadAsync(ct))
        {
            res += $"[Outbox: Topic={reader["Topic"]}, Published={reader["PublishedAtUtc"]}, Payload={reader["Payload"]}]\n";
        }
        return res;
    }

    public static async Task<string> GetNotificationDebugAsync(CancellationToken ct = default)
    {
        const string connStr = "Server=ceylonquest-mysql-db.mysql.database.azure.com;Port=3306;Database=payment_notification_db;User=payment_user;Password= chanumim@082624101636;SslMode=Required;";
        await using var connection = new MySqlConnection(connStr);
        await connection.OpenAsync(ct);
        const string sql = "SELECT Topic, AttemptCount, Resolved, Error FROM FailedEvents ORDER BY LastAttemptAtUtc DESC LIMIT 3";
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var res = "";
        while (await reader.ReadAsync(ct))
        {
            res += $"[FailedEvent: Topic={reader["Topic"]}, Attempts={reader["AttemptCount"]}, Resolved={reader["Resolved"]}, Error={reader["Error"]}]\n";
        }
        return res;
    }
}
