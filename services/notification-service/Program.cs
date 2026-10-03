using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Shared.Kafka;
using NotificationService.Data;
using NotificationService.Services;
using NotificationHandler = NotificationService.Services.NotificationService;

var builder = WebApplication.CreateBuilder(args);


// =========================================================
// Controllers + Enum JSON conversion
// =========================================================

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()
        );
    });

// =========================================================
// Database
// =========================================================

var connectionString =
    builder.Configuration.GetConnectionString("PaymentNotificationDb")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:PaymentNotificationDb using user secrets or environment variables.");

var serverVersion =
    Version.TryParse(
        builder.Configuration["DatabaseServerVersion"],
        out var parsedVersion)
        ? new MySqlServerVersion(parsedVersion)
        : new MySqlServerVersion(new Version(8, 0, 30));

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseMySql(connectionString, serverVersion));


// =========================================================
// JWT Authentication
// =========================================================

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;

    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;

    var jwtKey =
        builder.Configuration["Jwt:Key"];

    var jwtIssuer =
        builder.Configuration["Jwt:Issuer"]
        ?? "CeylonQuest";

    var jwtAudience =
        builder.Configuration["Jwt:Audience"]
        ?? "CeylonQuestAudience";

    if (string.IsNullOrWhiteSpace(jwtKey))
    {
        if (!builder.Environment.IsDevelopment())
            throw new InvalidOperationException("Configure Jwt:Key to match identity-service.");
        jwtKey =
            "dev_secret_do_not_use_in_production_please_change_which_is_long_enough";
    }

    using var sha = SHA256.Create();

    var signingKeyBytes =
        sha.ComputeHash(
            Encoding.UTF8.GetBytes(jwtKey)
        );

    options.TokenValidationParameters =
        new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,

            IssuerSigningKey =
                new SymmetricSecurityKey(signingKeyBytes),

            RoleClaimType =
                ClaimTypes.Role,

            NameClaimType =
                ClaimTypes.NameIdentifier
        };
});

builder.Services.AddAuthorization();


// =========================================================
// Swagger
// =========================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "CeylonQuest Notification Service API",
            Version = "v1"
        });

    // JWT Bearer authentication in Swagger
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT access token."
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference =
                        new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                },
                Array.Empty<string>()
            }
        });
});

// ===== CORS =====
const string FrontendPolicy = "FrontendPolicy";

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] {
                         "http://localhost:5173",
                         "http://localhost:5000",
                         "https://jolly-field-0aaea8a00.7.azurestaticapps.net"
                     };

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendPolicy, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddKafka(builder.Configuration);

builder.Services.AddHttpClient<IRecipientResolver, RecipientResolver>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:ProviderCatalog"] ?? "http://localhost:5141");
});
builder.Services.AddScoped<NotificationEventProcessor>();
builder.Services.AddScoped<NotificationHandler>();
builder.Services.AddHostedService<NotificationEventConsumer>();

var app = builder.Build();

// ====== HTTP request pipeline=====
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(FrontendPolicy);

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
