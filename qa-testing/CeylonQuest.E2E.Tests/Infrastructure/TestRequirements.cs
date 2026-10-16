using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace CeylonQuest.E2E.Tests.Infrastructure;

// Missing prerequisites are discovered as Skipped, never returned as a passing test.
public class VisitorFactAttribute : FactAttribute
{
    public VisitorFactAttribute() => Require("CQ_E2E_VISITOR_EMAIL", "CQ_E2E_VISITOR_PASSWORD");
    protected void Require(params string[] names)
    {
        var missing = names.Where(name => string.IsNullOrWhiteSpace(TestSettings.Optional(name))).ToArray();
        if (missing.Length > 0) Skip = "Missing environment variables: " + string.Join(", ", missing);
    }
}

public class AccommodationFactAttribute : VisitorFactAttribute
{
    public AccommodationFactAttribute()
    {
        if (Skip is null) Require("CQ_E2E_ACCOMMODATION_TITLE");
    }
}

public sealed class StayFactAttribute : AccommodationFactAttribute
{
    public StayFactAttribute()
    {
        if (Skip is null) Require("CQ_E2E_CHECK_IN", "CQ_E2E_CHECK_OUT");
    }
}

public sealed class BookingFactAttribute : AccommodationFactAttribute
{
    public BookingFactAttribute()
    {
        if (Skip is null) Require("CQ_E2E_CHECK_IN", "CQ_E2E_CHECK_OUT");
        if (Skip is null && !TestSettings.Enabled("CQ_E2E_ALLOW_BOOKING"))
            Skip = "Booking creation is disabled. Set CQ_E2E_ALLOW_BOOKING=true for a dedicated test listing.";
        if (Skip is null && !TestSettings.Enabled("CQ_E2E_STRIPE_TEST_MODE_CONFIRMED"))
            Skip = "An operator must confirm the deployed Stripe integration uses test mode before booking.";
        if (Skip is null && TestSettings.Headless)
            Skip = "Set CQ_E2E_HEADLESS=false: hosted Stripe test checkout requires manual completion.";
    }
}
