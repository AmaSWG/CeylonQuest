using System.Text.Json;

namespace CeylonQuest.Tests.Sprint4Notifications.Infrastructure;

internal static class JsonDefaults
{
    // Web defaults = camelCase + case-insensitive property matching, matching ASP.NET Core's
    // default JSON output for every CeylonQuest service.
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
