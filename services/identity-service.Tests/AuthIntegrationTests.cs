using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using IdentityService.Data;
using IdentityService.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace IdentityService.Tests;

public class AuthIntegrationTests
{
    public AuthIntegrationTests() { }

    private HttpClient CreateClientWithInMemoryDb(Action<ApplicationDbContext> seed)
    {
        // Ensure migrations are disabled for the test host via environment variable as well.
        Environment.SetEnvironmentVariable("DisableMigrations", "true");
        // Mark environment as Testing so Program can conditionally skip startup tasks.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        var factory = new WebApplicationFactory<Program>();
        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });

                // build provider to seed
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureCreated();
                seed(db);
            });

            builder.ConfigureAppConfiguration((context, config) =>
            {
                var dict = new Dictionary<string, string>
                {
                    ["Jwt:Key"] = "test_dev_secret_ABCDEF0123456789_long_enough_32bytes",
                    ["Jwt:Issuer"] = "TestIssuer",
                    ["Jwt:Audience"] = "TestAudience",
                    ["Jwt:ExpiryMinutes"] = "60"
                };
                dict["DisableMigrations"] = "true";
                config.AddInMemoryCollection(dict);
            });
        }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        return client;
    }

    [Fact]
    public async Task SuccessfulLogin_ReturnsTokenAndRoleVisitor()
    {
        var client = CreateClientWithInMemoryDb(db =>
        {
            var hasher = new PasswordHasher<User>();
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                PhoneNumber = "123",
                Nationality = "LK",
                Role = UserRole.Visitor,
            };
            user.PasswordHash = hasher.HashPassword(user, "Password123!");
            db.Users.Add(user);
            db.SaveChanges();
        });

        var req = new { Email = "test@example.com", Password = "Password123!" };
        var res = await client.PostAsync("/api/auth/login", new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json"));
        Assert.True(res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());

        var body = JsonSerializer.Deserialize<JsonElement>(await res.Content.ReadAsStringAsync());
        var token = body.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var role = jwt.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
        Assert.Equal("Visitor", role);
    }

    [Fact]
    public async Task InvalidCredentials_ReturnsUnauthorized()
    {
        var client = CreateClientWithInMemoryDb(db =>
        {
            var hasher = new PasswordHasher<User>();
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test2@example.com",
                FirstName = "Test",
                LastName = "User",
                PhoneNumber = "123",
                Nationality = "LK",
                Role = UserRole.Visitor,
            };
            user.PasswordHash = hasher.HashPassword(user, "Password123!");
            db.Users.Add(user);
            db.SaveChanges();
        });

        var req = new { Email = "test2@example.com", Password = "WrongPassword" };
        var res = await client.PostAsync("/api/auth/login", new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json"));
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Logout_RequiresAuthentication()
    {
        var client = CreateClientWithInMemoryDb(db =>
        {
            var hasher = new PasswordHasher<User>();
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test3@example.com",
                FirstName = "Test",
                LastName = "User",
                PhoneNumber = "123",
                Nationality = "LK",
                Role = UserRole.Visitor,
            };
            user.PasswordHash = hasher.HashPassword(user, "Password123!");
            db.Users.Add(user);
            db.SaveChanges();
        });

        // unauthenticated logout should be 401
        var res = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, res.StatusCode);

        // login to get token
        var loginReq = new { Email = "test3@example.com", Password = "Password123!" };
        var loginRes = await client.PostAsync("/api/auth/login", new StringContent(JsonSerializer.Serialize(loginReq), Encoding.UTF8, "application/json"));
        loginRes.EnsureSuccessStatusCode();
        var body = JsonSerializer.Deserialize<JsonElement>(await loginRes.Content.ReadAsStringAsync());
        var token = body.GetProperty("accessToken").GetString();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var logoutRes = await client.PostAsync("/api/auth/logout", null);
        Assert.True(logoutRes.IsSuccessStatusCode);
    }
}
