using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IdentityService.Data;
using IdentityService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace IdentityService.Tests;

public class ProviderActivationIntegrationTests
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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

    // ──────────────────────────────────────────────────────────────────────────
    // Helper: create a seeded provider user with a valid OTP
    // ──────────────────────────────────────────────────────────────────────────
    private static User SeedProviderWithOtp(
        ApplicationDbContext db,
        string email,
        string otpCode,
        DateTime otpExpiresAt)
    {
        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            Id                    = Guid.NewGuid(),
            Email                 = email,
            FirstName             = "Provider",
            LastName              = "Test",
            PhoneNumber           = "0771234567",
            Nationality           = "LK",
            Role                  = UserRole.Provider,
            IsActive              = false,
            RequiresPasswordChange = true,
            OtpCode               = otpCode,
            OtpExpiresAt          = otpExpiresAt
        };
        // Password hash is empty at this stage — the provider has not yet set one.
        user.PasswordHash = hasher.HashPassword(user, Guid.NewGuid().ToString());
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test 1: Successful activation
    // ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task SuccessfulActivation_Returns200_AndAccountIsActive()
    {
        const string email = "provider_activate_ok@example.com";
        const string otp   = "654321";

        var client = CreateClientWithInMemoryDb(db =>
        {
            SeedProviderWithOtp(db, email, otp, DateTime.UtcNow.AddMinutes(15));
        }, "ProviderActivateDb_Success");

        var req = new { Email = email, Otp = otp, NewPassword = "NewSecure@99" };
        var res = await client.PostAsync("/api/auth/provider/activate", Json(req));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = JsonSerializer.Deserialize<JsonElement>(
            await res.Content.ReadAsStringAsync(), _jsonOptions);
        Assert.True(body.TryGetProperty("message", out var msg));
        Assert.False(string.IsNullOrWhiteSpace(msg.GetString()));

        // Verify the user can now log in with the new password.
        var loginReq = new { Email = email, Password = "NewSecure@99" };
        var loginRes = await client.PostAsync("/api/auth/login", Json(loginReq));
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);

        var loginBody = JsonSerializer.Deserialize<JsonElement>(
            await loginRes.Content.ReadAsStringAsync(), _jsonOptions);
        var token = loginBody.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test 2: Invalid OTP
    // ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task InvalidOtp_Returns401()
    {
        const string email = "provider_activate_badotp@example.com";
        const string otp   = "111111";

        var client = CreateClientWithInMemoryDb(db =>
        {
            SeedProviderWithOtp(db, email, otp, DateTime.UtcNow.AddMinutes(15));
        }, "ProviderActivateDb_InvalidOtp");

        var req = new { Email = email, Otp = "999999", NewPassword = "NewSecure@99" };
        var res = await client.PostAsync("/api/auth/provider/activate", Json(req));

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test 3: Expired OTP
    // ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task ExpiredOtp_Returns400()
    {
        const string email = "provider_activate_expired@example.com";
        const string otp   = "222222";

        var client = CreateClientWithInMemoryDb(db =>
        {
            // Seed an OTP that expired 1 minute ago.
            SeedProviderWithOtp(db, email, otp, DateTime.UtcNow.AddMinutes(-1));
        }, "ProviderActivateDb_ExpiredOtp");

        var req = new { Email = email, Otp = otp, NewPassword = "NewSecure@99" };
        var res = await client.PostAsync("/api/auth/provider/activate", Json(req));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var body = JsonSerializer.Deserialize<JsonElement>(
            await res.Content.ReadAsStringAsync(), _jsonOptions);
        var msg = body.GetProperty("message").GetString() ?? string.Empty;
        Assert.Contains("expired", msg, StringComparison.OrdinalIgnoreCase);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test 4: Unknown email treated as invalid OTP (no email enumeration)
    // ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task UnknownEmail_Returns401_NotRevealingEmailExistence()
    {
        var client = CreateClientWithInMemoryDb(_ => { }, "ProviderActivateDb_UnknownEmail");

        var req = new { Email = "nobody@example.com", Otp = "000000", NewPassword = "NewSecure@99" };
        var res = await client.PostAsync("/api/auth/provider/activate", Json(req));

        // Must return 401 (same as invalid OTP) — not 404, to avoid email enumeration.
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Test 5: Missing required fields returns validation error
    // ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task MissingFields_ReturnsValidationProblem()
    {
        var client = CreateClientWithInMemoryDb(_ => { }, "ProviderActivateDb_MissingFields");

        // Send empty body
        var req = new { };
        var res = await client.PostAsync("/api/auth/provider/activate", Json(req));

        // ModelState validation should kick in.
        Assert.True(
            res.StatusCode == HttpStatusCode.BadRequest ||
            res.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Expected 400 or 422, got {res.StatusCode}");
    }
}
