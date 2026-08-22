using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IdentityService.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdentityService.Tests;

public class RegistrationIntegrationTests
{
    private HttpClient CreateClient()
    {
        Environment.SetEnvironmentVariable("DisableMigrations", "true");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        var factory = new WebApplicationFactory<Program>();
        var dbName = Guid.NewGuid().ToString();
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                });

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
            });

            builder.ConfigureAppConfiguration((context, config) =>
            {
                var dict = new Dictionary<string, string>
                {
                    ["Jwt:Key"] = "test_dev_secret_ABCDEF0123456789_long_enough_32bytes",
                    ["Jwt:Issuer"] = "TestIssuer",
                    ["Jwt:Audience"] = "TestAudience",
                    ["Jwt:ExpiryMinutes"] = "60",
                    ["DisableMigrations"] = "true"
                };
                config.AddInMemoryCollection(dict);
            });
        }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    private static object ValidPayload(string email = "visitor@example.com") => new
    {
        firstName = "Ann",
        lastName = "Perera",
        email,
        phoneNumber = "0771234567",
        nationality = "Sri Lankan",
        password = "Str0ng!Pass",
        confirmPassword = "Str0ng!Pass",
        registrationType = "Visitor"
    };

    private static StringContent Json(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    // Scenario 1: Successful Registration
    [Fact]
    public async Task Register_ValidRequest_ReturnsCreated()
    {
        var client = CreateClient();

        var res = await client.PostAsync("/api/auth/register", Json(ValidPayload()));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var body = JsonSerializer.Deserialize<JsonElement>(await res.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("message").GetString()));
    }

    // Scenario 2: Duplicate Email
    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var client = CreateClient();

        var first = await client.PostAsync("/api/auth/register", Json(ValidPayload("dup@example.com")));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsync("/api/auth/register", Json(ValidPayload("dup@example.com")));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    // Scenario 3: Weak Password
    [Theory]
    [InlineData("short1!")]
    [InlineData("alllowercase1!")]
    [InlineData("ALLUPPERCASE1!")]
    [InlineData("NoDigitsHere!")]
    [InlineData("NoSpecialChar123")]
    public async Task Register_WeakPassword_ReturnsBadRequest(string weakPassword)
    {
        var client = CreateClient();
        var payload = ValidPayload();
        var json = JsonSerializer.Serialize(payload).Replace("Str0ng!Pass", weakPassword);

        var res = await client.PostAsync("/api/auth/register",
            new StringContent(json, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Theory]
    [InlineData("firstName")]
    [InlineData("lastName")]
    [InlineData("email")]
    [InlineData("phoneNumber")]
    [InlineData("nationality")]
    public async Task Register_MissingRequiredField_ReturnsBadRequest(string fieldToBlank)
    {
        var client = CreateClient();
        var dict = new Dictionary<string, object>
        {
            ["firstName"] = "Ann",
            ["lastName"] = "Perera",
            ["email"] = "required@example.com",
            ["phoneNumber"] = "0771234567",
            ["nationality"] = "Sri Lankan",
            ["password"] = "Str0ng!Pass",
            ["confirmPassword"] = "Str0ng!Pass",
            ["registrationType"] = "Visitor"
        };
        dict[fieldToBlank] = "";

        var res = await client.PostAsync("/api/auth/register", Json(dict));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_PasswordAndConfirmMismatch_ReturnsBadRequest()
    {
        var client = CreateClient();
        var payload = new
        {
            firstName = "Ann",
            lastName = "Perera",
            email = "mismatch@example.com",
            phoneNumber = "0771234567",
            nationality = "Sri Lankan",
            password = "Str0ng!Pass",
            confirmPassword = "Different!Pass1",
            registrationType = "Visitor"
        };

        var res = await client.PostAsync("/api/auth/register", Json(payload));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
