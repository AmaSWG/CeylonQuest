using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IdentityService.Data;
using IdentityService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdentityService.Tests;

/// <summary>
/// Integration tests for the Provider Dashboard endpoints:
///   GET/POST /api/provider/timeslots
///   PUT/DELETE /api/provider/timeslots/{id}
///   GET/POST /api/provider/prices
///   PUT/DELETE /api/provider/prices/{id}
///   GET /api/provider/info
///
/// Tests cover:
///   - Successful access by an authenticated Provider
///   - 403 Forbidden for authenticated Visitors (role enforcement)
///   - 401 Unauthorized for unauthenticated requests
///   - Data isolation: a Provider cannot mutate another Provider's records
/// </summary>
public class ProviderDashboardIntegrationTests
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ─────────────────────────────────────────────────────────────────────────
    // Test infrastructure helpers
    // ─────────────────────────────────────────────────────────────────────────

    private HttpClient CreateClientWithInMemoryDb(Action<ApplicationDbContext> seed, string dbName)
    {
        Environment.SetEnvironmentVariable("DisableMigrations", "true");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        var factory = new WebApplicationFactory<Program>();
        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureCreated();
                seed(db);
            });

            builder.ConfigureAppConfiguration((_, config) =>
            {
                var dict = new Dictionary<string, string?>
                {
                    ["Jwt:Key"]           = "test_dev_secret_ABCDEF0123456789_long_enough_32bytes",
                    ["Jwt:Issuer"]        = "TestIssuer",
                    ["Jwt:Audience"]      = "TestAudience",
                    ["Jwt:ExpiryMinutes"] = "60",
                    ["DisableMigrations"] = "true"
                };
                config.AddInMemoryCollection(dict);
            });
        }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        return client;
    }

    private static StringContent Json(object obj) =>
        new StringContent(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");

    /// <summary>Seeds a user with a hashed password and returns the User entity.</summary>
    private static User SeedUser(
        ApplicationDbContext db,
        string email,
        string password,
        UserRole role = UserRole.Visitor)
    {
        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            Id          = Guid.NewGuid(),
            Email       = email,
            FirstName   = "Test",
            LastName    = "User",
            PhoneNumber = "0771234567",
            Nationality = "Sri Lankan",
            Role        = role,
            IsActive    = true,
            CreatedAt   = DateTime.UtcNow
        };
        user.PasswordHash = hasher.HashPassword(user, password);
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }



    /// <summary>Logs in and returns the Bearer token.</summary>
    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var resp = await client.PostAsync("/api/auth/login", Json(new { Email = email, Password = password }));
        resp.EnsureSuccessStatusCode();
        var body = JsonSerializer.Deserialize<JsonElement>(await resp.Content.ReadAsStringAsync(), _jsonOptions);
        return body.GetProperty("accessToken").GetString()!;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // /api/provider/info — GET
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetProviderInfo_AsProvider_Returns200()
    {
        const string email = "provider_info_ok@example.com";
        const string pass  = "Password123!";

        var client = CreateClientWithInMemoryDb(db =>
        {
            SeedUser(db, email, pass, UserRole.Provider);
            // Use Rejected status so AuthService doesn't block the standard password login path.
            // The ProviderInfoController still finds this record and returns its business fields.
            db.ProviderApplications.Add(new ProviderApplication
            {
                Id           = Guid.NewGuid(),
                FirstName    = "Test",
                LastName     = "Provider",
                Email        = email,
                PhoneNumber  = "0771234567",
                BusinessName = "Adventure Sri Lanka",
                ServiceType  = "Wildlife Safari",
                Location     = "Yala, Sri Lanka",
                Description  = "Professional wildlife safari tours.",
                Status       = ProviderApplicationStatus.Rejected,
                CreatedAt    = DateTime.UtcNow
            });
            db.SaveChanges();
        }, "ProviderDash_Info_OK");

        var token = await LoginAsync(client, email, pass);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/provider/info");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = JsonSerializer.Deserialize<JsonElement>(await res.Content.ReadAsStringAsync(), _jsonOptions);
        Assert.Equal(email, body.GetProperty("email").GetString());
        Assert.Equal("Adventure Sri Lanka", body.GetProperty("businessName").GetString());
        Assert.Equal("Wildlife Safari",     body.GetProperty("serviceType").GetString());
    }

    [Fact]
    public async Task GetProviderInfo_AsProvider_WithNoApplication_Returns200WithEmptyBusinessFields()
    {
        const string email = "provider_info_noapp@example.com";
        const string pass  = "Password123!";

        var client = CreateClientWithInMemoryDb(db =>
        {
            SeedUser(db, email, pass, UserRole.Provider);
        }, "ProviderDash_Info_NoApp");

        var token = await LoginAsync(client, email, pass);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/provider/info");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = JsonSerializer.Deserialize<JsonElement>(await res.Content.ReadAsStringAsync(), _jsonOptions);
        Assert.Equal(email,         body.GetProperty("email").GetString());
        Assert.Equal(string.Empty,  body.GetProperty("businessName").GetString());
        Assert.Equal(string.Empty,  body.GetProperty("serviceType").GetString());
    }

    [Fact]
    public async Task GetProviderInfo_AsVisitor_Returns403()
    {
        const string email = "visitor_info_403@example.com";
        const string pass  = "Password123!";

        var client = CreateClientWithInMemoryDb(db =>
        {
            SeedUser(db, email, pass, UserRole.Visitor);
        }, "ProviderDash_Info_Visitor403");

        var token = await LoginAsync(client, email, pass);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/provider/info");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task GetProviderInfo_Unauthenticated_Returns401()
    {
        var client = CreateClientWithInMemoryDb(_ => { }, "ProviderDash_Info_Unauth");
        var res = await client.GetAsync("/api/provider/info");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // /api/provider/timeslots — GET
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTimeSlots_AsProvider_Returns200()
    {
        const string email = "provider_slots_get@example.com";
        const string pass  = "Password123!";

        var client = CreateClientWithInMemoryDb(db =>
        {
            var user = SeedUser(db, email, pass, UserRole.Provider);
            db.ProviderTimeSlots.Add(new ProviderTimeSlot
            {
                Id          = Guid.NewGuid(),
                ProviderId  = user.Id,
                Date        = "2026-09-01",
                StartTime   = "09:00",
                EndTime     = "11:00",
                IsAvailable = true,
                CreatedAt   = DateTime.UtcNow
            });
            db.SaveChanges();
        }, "ProviderDash_Slots_Get");

        var token = await LoginAsync(client, email, pass);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/provider/timeslots");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = JsonSerializer.Deserialize<JsonElement>(await res.Content.ReadAsStringAsync(), _jsonOptions);
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        Assert.Equal(1, body.GetArrayLength());
        Assert.Equal("2026-09-01", body[0].GetProperty("date").GetString());
    }

    [Fact]
    public async Task GetTimeSlots_AsVisitor_Returns403()
    {
        const string email = "visitor_slots_403@example.com";
        const string pass  = "Password123!";

        var client = CreateClientWithInMemoryDb(db =>
        {
            SeedUser(db, email, pass, UserRole.Visitor);
        }, "ProviderDash_Slots_Visitor403");

        var token = await LoginAsync(client, email, pass);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/provider/timeslots");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task GetTimeSlots_Unauthenticated_Returns401()
    {
        var client = CreateClientWithInMemoryDb(_ => { }, "ProviderDash_Slots_Unauth");
        var res = await client.GetAsync("/api/provider/timeslots");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // /api/provider/timeslots — POST
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddTimeSlot_AsProvider_Returns201()
    {
        const string email = "provider_slot_add@example.com";
        const string pass  = "Password123!";

        var client = CreateClientWithInMemoryDb(db =>
        {
            SeedUser(db, email, pass, UserRole.Provider);
        }, "ProviderDash_Slot_Add");

        var token = await LoginAsync(client, email, pass);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var req = new { Date = "2026-09-05", StartTime = "10:00", EndTime = "12:00", IsAvailable = true };
        var res = await client.PostAsync("/api/provider/timeslots", Json(req));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);

        var body = JsonSerializer.Deserialize<JsonElement>(await res.Content.ReadAsStringAsync(), _jsonOptions);
        Assert.Equal("2026-09-05", body.GetProperty("date").GetString());
        Assert.Equal("10:00",      body.GetProperty("startTime").GetString());
    }

    [Fact]
    public async Task AddTimeSlot_AsVisitor_Returns403()
    {
        const string email = "visitor_slot_add_403@example.com";
        const string pass  = "Password123!";

        var client = CreateClientWithInMemoryDb(db =>
        {
            SeedUser(db, email, pass, UserRole.Visitor);
        }, "ProviderDash_Slot_AddVisitor403");

        var token = await LoginAsync(client, email, pass);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var req = new { Date = "2026-09-05", StartTime = "10:00", EndTime = "12:00", IsAvailable = true };
        var res = await client.PostAsync("/api/provider/timeslots", Json(req));

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // /api/provider/timeslots/{id} — PUT
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateTimeSlot_AsProvider_OwnSlot_Returns200()
    {
        const string email = "provider_slot_update@example.com";
        const string pass  = "Password123!";
        var slotId = Guid.NewGuid();

        var client = CreateClientWithInMemoryDb(db =>
        {
            var user = SeedUser(db, email, pass, UserRole.Provider);
            db.ProviderTimeSlots.Add(new ProviderTimeSlot
            {
                Id          = slotId,
                ProviderId  = user.Id,
                Date        = "2026-09-10",
                StartTime   = "08:00",
                EndTime     = "10:00",
                IsAvailable = true,
                CreatedAt   = DateTime.UtcNow
            });
            db.SaveChanges();
        }, "ProviderDash_Slot_Update");

        var token = await LoginAsync(client, email, pass);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var req = new { Date = "2026-09-10", StartTime = "09:00", EndTime = "11:00", IsAvailable = false };
        var res = await client.PutAsync($"/api/provider/timeslots/{slotId}", Json(req));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = JsonSerializer.Deserialize<JsonElement>(await res.Content.ReadAsStringAsync(), _jsonOptions);
        Assert.Equal("09:00", body.GetProperty("startTime").GetString());
        Assert.False(body.GetProperty("isAvailable").GetBoolean());
    }

    [Fact]
    public async Task UpdateTimeSlot_AsProvider_OtherProvidersSlot_Returns404()
    {
        const string emailA = "provider_slot_a@example.com";
        const string emailB = "provider_slot_b@example.com";
        const string pass   = "Password123!";
        var slotId = Guid.NewGuid();

        var client = CreateClientWithInMemoryDb(db =>
        {
            var providerA = SeedUser(db, emailA, pass, UserRole.Provider);
            SeedUser(db, emailB, pass, UserRole.Provider);

            // The slot belongs to Provider A
            db.ProviderTimeSlots.Add(new ProviderTimeSlot
            {
                Id          = slotId,
                ProviderId  = providerA.Id,
                Date        = "2026-09-15",
                StartTime   = "14:00",
                EndTime     = "16:00",
                IsAvailable = true,
                CreatedAt   = DateTime.UtcNow
            });
            db.SaveChanges();
        }, "ProviderDash_Slot_Isolation");

        // Login as Provider B
        var token = await LoginAsync(client, emailB, pass);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var req = new { Date = "2026-09-15", StartTime = "15:00", EndTime = "17:00", IsAvailable = false };
        var res = await client.PutAsync($"/api/provider/timeslots/{slotId}", Json(req));

        // Provider B must not see Provider A's slot — expect 404
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // /api/provider/timeslots/{id} — DELETE
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteTimeSlot_AsProvider_Returns204()
    {
        const string email = "provider_slot_delete@example.com";
        const string pass  = "Password123!";
        var slotId = Guid.NewGuid();

        var client = CreateClientWithInMemoryDb(db =>
        {
            var user = SeedUser(db, email, pass, UserRole.Provider);
            db.ProviderTimeSlots.Add(new ProviderTimeSlot
            {
                Id          = slotId,
                ProviderId  = user.Id,
                Date        = "2026-09-20",
                StartTime   = "10:00",
                EndTime     = "12:00",
                IsAvailable = true,
                CreatedAt   = DateTime.UtcNow
            });
            db.SaveChanges();
        }, "ProviderDash_Slot_Delete");

        var token = await LoginAsync(client, email, pass);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await client.DeleteAsync($"/api/provider/timeslots/{slotId}");
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        // Verify it's gone
        var getRes = await client.GetAsync("/api/provider/timeslots");
        var body = JsonSerializer.Deserialize<JsonElement>(await getRes.Content.ReadAsStringAsync(), _jsonOptions);
        Assert.Equal(0, body.GetArrayLength());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // /api/provider/prices — GET
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPrices_AsProvider_Returns200()
    {
        const string email = "provider_prices_get@example.com";
        const string pass  = "Password123!";

        var client = CreateClientWithInMemoryDb(db =>
        {
            var user = SeedUser(db, email, pass, UserRole.Provider);
            db.ProviderServicePrices.Add(new ProviderServicePrice
            {
                Id           = Guid.NewGuid(),
                ProviderId   = user.Id,
                ServiceName  = "Wildlife Safari",
                Description  = "Full-day tour",
                PricePerUnit = 5000m,
                Unit         = "per person",
                UpdatedAt    = DateTime.UtcNow
            });
            db.SaveChanges();
        }, "ProviderDash_Prices_Get");

        var token = await LoginAsync(client, email, pass);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/provider/prices");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = JsonSerializer.Deserialize<JsonElement>(await res.Content.ReadAsStringAsync(), _jsonOptions);
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        Assert.Equal(1, body.GetArrayLength());
        Assert.Equal("Wildlife Safari", body[0].GetProperty("serviceName").GetString());
        Assert.Equal(5000m, body[0].GetProperty("pricePerUnit").GetDecimal());
    }

    [Fact]
    public async Task GetPrices_AsVisitor_Returns403()
    {
        const string email = "visitor_prices_403@example.com";
        const string pass  = "Password123!";

        var client = CreateClientWithInMemoryDb(db =>
        {
            SeedUser(db, email, pass, UserRole.Visitor);
        }, "ProviderDash_Prices_Visitor403");

        var token = await LoginAsync(client, email, pass);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/provider/prices");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // /api/provider/prices — POST
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddPrice_AsProvider_Returns201()
    {
        const string email = "provider_price_add@example.com";
        const string pass  = "Password123!";

        var client = CreateClientWithInMemoryDb(db =>
        {
            SeedUser(db, email, pass, UserRole.Provider);
        }, "ProviderDash_Price_Add");

        var token = await LoginAsync(client, email, pass);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var req = new { ServiceName = "Guided Trek", Description = "Half-day trek", PricePerUnit = 3500.00, Unit = "per person" };
        var res = await client.PostAsync("/api/provider/prices", Json(req));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);

        var body = JsonSerializer.Deserialize<JsonElement>(await res.Content.ReadAsStringAsync(), _jsonOptions);
        Assert.Equal("Guided Trek", body.GetProperty("serviceName").GetString());
        Assert.Equal(3500m,         body.GetProperty("pricePerUnit").GetDecimal());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // /api/provider/prices/{id} — PUT
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePrice_AsProvider_Returns200()
    {
        const string email = "provider_price_update@example.com";
        const string pass  = "Password123!";
        var priceId = Guid.NewGuid();

        var client = CreateClientWithInMemoryDb(db =>
        {
            var user = SeedUser(db, email, pass, UserRole.Provider);
            db.ProviderServicePrices.Add(new ProviderServicePrice
            {
                Id           = priceId,
                ProviderId   = user.Id,
                ServiceName  = "Safari",
                Description  = "Old desc",
                PricePerUnit = 4000m,
                Unit         = "per person",
                UpdatedAt    = DateTime.UtcNow
            });
            db.SaveChanges();
        }, "ProviderDash_Price_Update");

        var token = await LoginAsync(client, email, pass);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var req = new { ServiceName = "Premium Safari", Description = "New desc", PricePerUnit = 6000.00, Unit = "per person" };
        var res = await client.PutAsync($"/api/provider/prices/{priceId}", Json(req));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = JsonSerializer.Deserialize<JsonElement>(await res.Content.ReadAsStringAsync(), _jsonOptions);
        Assert.Equal("Premium Safari", body.GetProperty("serviceName").GetString());
        Assert.Equal(6000m,            body.GetProperty("pricePerUnit").GetDecimal());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // /api/provider/prices/{id} — DELETE
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeletePrice_AsProvider_Returns204()
    {
        const string email = "provider_price_delete@example.com";
        const string pass  = "Password123!";
        var priceId = Guid.NewGuid();

        var client = CreateClientWithInMemoryDb(db =>
        {
            var user = SeedUser(db, email, pass, UserRole.Provider);
            db.ProviderServicePrices.Add(new ProviderServicePrice
            {
                Id           = priceId,
                ProviderId   = user.Id,
                ServiceName  = "Boat Tour",
                Description  = string.Empty,
                PricePerUnit = 2500m,
                Unit         = "per boat",
                UpdatedAt    = DateTime.UtcNow
            });
            db.SaveChanges();
        }, "ProviderDash_Price_Delete");

        var token = await LoginAsync(client, email, pass);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await client.DeleteAsync($"/api/provider/prices/{priceId}");
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

        // Verify it's gone
        var getRes = await client.GetAsync("/api/provider/prices");
        var body = JsonSerializer.Deserialize<JsonElement>(await getRes.Content.ReadAsStringAsync(), _jsonOptions);
        Assert.Equal(0, body.GetArrayLength());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Additional: Provider only sees their own data (prices isolation)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPrices_OnlyReturnsOwnProvidersPrices()
    {
        const string emailA = "priceisolation_a@example.com";
        const string emailB = "priceisolation_b@example.com";
        const string pass   = "Password123!";

        var client = CreateClientWithInMemoryDb(db =>
        {
            var providerA = SeedUser(db, emailA, pass, UserRole.Provider);
            var providerB = SeedUser(db, emailB, pass, UserRole.Provider);

            db.ProviderServicePrices.Add(new ProviderServicePrice
            {
                Id = Guid.NewGuid(), ProviderId = providerA.Id,
                ServiceName = "Tour A", PricePerUnit = 100m, Unit = "per person", UpdatedAt = DateTime.UtcNow
            });
            db.ProviderServicePrices.Add(new ProviderServicePrice
            {
                Id = Guid.NewGuid(), ProviderId = providerB.Id,
                ServiceName = "Tour B", PricePerUnit = 200m, Unit = "per person", UpdatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
        }, "ProviderDash_Price_Isolation");

        // Login as Provider A — should only see Tour A
        var tokenA = await LoginAsync(client, emailA, pass);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);

        var resA = await client.GetAsync("/api/provider/prices");
        var bodyA = JsonSerializer.Deserialize<JsonElement>(await resA.Content.ReadAsStringAsync(), _jsonOptions);

        Assert.Equal(1, bodyA.GetArrayLength());
        Assert.Equal("Tour A", bodyA[0].GetProperty("serviceName").GetString());
    }
}
