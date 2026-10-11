using CeylonQuest.Tests.Sprint4Notifications.Infrastructure;

namespace CeylonQuest.Tests.Sprint4Notifications.Api;

/// <summary>
/// Shared actors for the API integration suite. All assertions that use these actors are either
/// delta-based (before/after counts) or match a specific, unique BookingId/ReviewId, so sharing
/// the same three accounts across every test in the collection is safe and avoids re-registering
/// a user per test.
/// </summary>
public sealed class NotificationApiFixture : IAsyncLifetime
{
    public IdentityApiClient Identity { get; } = new();
    public NotificationApiClient Notifications { get; } = new();
    public KafkaEventPublisher Kafka { get; private set; } = null!;

    /// <summary>The "visitor" persona - a real identity-service account.</summary>
    public TestUser Visitor { get; private set; } = null!;

    /// <summary>
    /// Stands in for "the provider" recipient. Real provider onboarding requires OTP email
    /// activation plus admin approval (see services/identity-service/Services/ProviderActivationService.cs),
    /// which this harness cannot automate, so a second real identity-service account is used purely
    /// as a notification recipient - notification-service itself does not check roles.
    /// </summary>
    public TestUser ProviderPersona { get; private set; } = null!;

    /// <summary>An unrelated third account used for ownership/isolation checks.</summary>
    public TestUser Outsider { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Kafka = new KafkaEventPublisher();
        Visitor = await Identity.CreateVisitorAsync("api-visitor");
        ProviderPersona = await Identity.CreateVisitorAsync("api-provider-persona");
        Outsider = await Identity.CreateVisitorAsync("api-outsider");
    }

    public async Task DisposeAsync() => await Kafka.DisposeAsync();
}

[CollectionDefinition(nameof(NotificationApiCollection))]
public sealed class NotificationApiCollection : ICollectionFixture<NotificationApiFixture>
{
}
