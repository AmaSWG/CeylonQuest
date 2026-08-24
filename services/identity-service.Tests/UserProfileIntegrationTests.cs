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

public class UserProfileIntegrationTests
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
                var dict = new Dictionary<string, string>
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

    private static User SeedUser(ApplicationDbContext db, string email, string password, UserRole role = UserRole.Visitor)
    {
        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            Id          = Guid.NewGuid(),
            Email       = email,
            FirstName   = "Chaminda",
            LastName    = "Vaas",
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

    private static async Task<string> LoginAndGetTokenAsync(HttpClient client, string email, string password)
    {
        var loginReq = new { Email = email, Password = password };
        var loginRes = await client.PostAsync("/api/auth/login", Json(loginReq));
        loginRes.EnsureSuccessStatusCode();

        var body = JsonSerializer.Deserialize<JsonElement>(
            await loginRes.Content.ReadAsStringAsync(), _jsonOptions);
        return body.GetProperty("accessToken").GetString()!;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Scenario 1 – View Profile
    // ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task ViewProfile_AuthenticatedVisitor_ReturnsProfileWith200()
    {
        const string email = "chaminda@example.com";
        const string pass  = "Password123!";

        var client = CreateClientWithInMemoryDb(db =>
        {
            SeedUser(db, email, pass, UserRole.Visitor);
        }, "ProfileDb_ViewSuccess");

        var token = await LoginAndGetTokenAsync(client, email, pass);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = JsonSerializer.Deserialize<JsonElement>(
            await res.Content.ReadAsStringAsync(), _jsonOptions);

        Assert.Equal("Chaminda", body.GetProperty("firstName").GetString());
        Assert.Equal("Vaas", body.GetProperty("lastName").GetString());
        Assert.Equal(email, body.GetProperty("email").GetString());
        Assert.Equal("0771234567", body.GetProperty("phoneNumber").GetString());
        Assert.Equal("Sri Lankan", body.GetProperty("nationality").GetString());
        Assert.Equal("Visitor", body.GetProperty("role").GetString());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Scenario 2 – Update Profile (Valid data)
    // ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task UpdateProfile_ValidData_Returns200AndUpdatedProfile()
    {
        const string email = "chaminda_update@example.com";
        const string pass  = "Password123!";

        var client = CreateClientWithInMemoryDb(db =>
        {
            SeedUser(db, email, pass, UserRole.Visitor);
        }, "ProfileDb_UpdateSuccess");

        var token = await LoginAndGetTokenAsync(client, email, pass);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateReq = new
        {
            FirstName   = "Chaminda Updated",
            LastName    = "Vaas Updated",
            PhoneNumber = "0779998888",
            Nationality = "New Zealander"
        };

        var res = await client.PutAsync("/api/users/me", Json(updateReq));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = JsonSerializer.Deserialize<JsonElement>(
            await res.Content.ReadAsStringAsync(), _jsonOptions);

        Assert.True(body.TryGetProperty("message", out var msg));
        Assert.Equal("Profile updated successfully.", msg.GetString());

        var profile = body.GetProperty("profile");
        Assert.Equal("Chaminda Updated", profile.GetProperty("firstName").GetString());
        Assert.Equal("Vaas Updated", profile.GetProperty("lastName").GetString());
        Assert.Equal("0779998888", profile.GetProperty("phoneNumber").GetString());
        Assert.Equal("New Zealander", profile.GetProperty("nationality").GetString());
        Assert.Equal(email, profile.GetProperty("email").GetString());
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Scenario 3 – Invalid Profile Information (Blank fields)
    // ──────────────────────────────────────────────────────────────────────────
    [Theory]
    [InlineData("", "LastName", "0771234567", "Sri Lankan")]
    [InlineData("FirstName", "", "0771234567", "Sri Lankan")]
    [InlineData("FirstName", "LastName", "", "Sri Lankan")]
    [InlineData("FirstName", "LastName", "0771234567", "")]
    public async Task UpdateProfile_InvalidBlankFields_ReturnsBadRequest(
        string firstName, string lastName, string phone, string nationality)
    {
        const string email = "chaminda_invalid@example.com";
        const string pass  = "Password123!";

        var client = CreateClientWithInMemoryDb(db =>
        {
            SeedUser(db, email, pass, UserRole.Visitor);
        }, $"ProfileDb_InvalidFields_{Guid.NewGuid()}");

        var token = await LoginAndGetTokenAsync(client, email, pass);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var invalidReq = new
        {
            FirstName   = firstName,
            LastName    = lastName,
            PhoneNumber = phone,
            Nationality = nationality
        };

        var res = await client.PutAsync("/api/users/me", Json(invalidReq));
        Assert.True(
            res.StatusCode == HttpStatusCode.BadRequest ||
            res.StatusCode == HttpStatusCode.UnprocessableEntity,
            $"Expected 400 or 422, got {res.StatusCode}");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Scenario 4 – Unauthenticated Access
    // ──────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task ViewProfile_Unauthenticated_ReturnsUnauthorized()
    {
        var client = CreateClientWithInMemoryDb(_ => { }, "ProfileDb_UnauthView");

        var res = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_Unauthenticated_ReturnsUnauthorized()
    {
        var client = CreateClientWithInMemoryDb(_ => { }, "ProfileDb_UnauthUpdate");

        var updateReq = new
        {
            FirstName   = "Name",
            LastName    = "Last",
            PhoneNumber = "0771234567",
            Nationality = "LK"
        };

        var res = await client.PutAsync("/api/users/me", Json(updateReq));
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
