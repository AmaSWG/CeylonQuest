namespace CeylonQuest.Tests.Sprint4Notifications.Infrastructure;

/// <summary>Small polling helper for UI assertions that depend on async Kafka consumption / React re-renders.</summary>
public static class Eventually
{
    public static void Assert(Func<bool> condition, TimeSpan? timeout = null, TimeSpan? interval = null, string? message = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TestConfig.EventualConsistencyTimeout);
        Exception? last = null;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                if (condition()) return;
            }
            catch (Exception ex)
            {
                last = ex;
            }
            Thread.Sleep(interval ?? TestConfig.PollInterval);
        }
        throw new TimeoutException(message ?? "Condition was not met within the allotted time.", last);
    }
}
