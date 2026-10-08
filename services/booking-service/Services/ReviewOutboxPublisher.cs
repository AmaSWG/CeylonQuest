using BookingService.Data;
using BookingService.Events;
using Microsoft.EntityFrameworkCore;
using Shared.Kafka;
using System.Text.Json;

namespace BookingService.Services;

public class ReviewOutboxPublisher(BookingDbContext db, IKafkaProducer kafka, TimeProvider clock)
{
    public async Task PublishAsync(CancellationToken token = default)
    {
        var messages = await db.ReviewOutboxMessages.Where(m => m.PublishedAtUtc == null).Take(100).ToListAsync(token);
        foreach (var message in messages)
        {
            var evt = JsonSerializer.Deserialize<ReviewSubmittedEvent>(message.Payload)
                ?? throw new JsonException("Invalid review outbox message.");
            await kafka.PublishAsync("review.submitted", evt.ListingId.ToString(), evt, token);
            message.PublishedAtUtc = clock.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync(token);
        }
    }
}
