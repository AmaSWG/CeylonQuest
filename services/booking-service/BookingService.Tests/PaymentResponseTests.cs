using BookingService.DTOs;
using Xunit;

namespace BookingService.Tests;

public class PaymentResponseTests
{
    [Fact]
    public void PaymentResponse_Properties_CanBeAssignedAndRetrieved()
    {
        var transactionId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var response = new PaymentResponse
        {
            TransactionId = transactionId,
            BookingId = bookingId,
            ListingTitle = "Ella Rock Hike",
            Amount = 15000m,
            TransactionReference = "PAY-123456",
            PaymentStatus = "Paid",
            BookingStatus = "Confirmed",
            Message = "Payment successful",
            ProcessedAt = now
        };

        Assert.Equal(transactionId, response.TransactionId);
        Assert.Equal(bookingId, response.BookingId);
        Assert.Equal("Ella Rock Hike", response.ListingTitle);
        Assert.Equal(15000m, response.Amount);
        Assert.Equal("PAY-123456", response.TransactionReference);
        Assert.Equal("Paid", response.PaymentStatus);
        Assert.Equal("Confirmed", response.BookingStatus);
        Assert.Equal("Payment successful", response.Message);
        Assert.Equal(now, response.ProcessedAt);
    }
}