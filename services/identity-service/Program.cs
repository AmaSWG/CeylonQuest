using IdentityService.Data;
using IdentityService.Services;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Shared.Kafka;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS for the SPA. Origins come from config (Cors:AllowedOrigins) so each
// environment can set its own; the defaults cover local dev. Credentials are
// allowed because the frontend sends requests with credentials: 'include'.
const string FrontendPolicy = "FrontendPolicy";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://localhost:5173", "http://localhost:5000" };
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendPolicy, policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        var jwtKey = builder.Configuration["Jwt:Key"];
        var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CeylonQuest";
        var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CeylonQuestAudience";
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            jwtKey = "dev_secret_do_not_use_in_production_please_change_which_is_long_enough";
        }

        using var sha = System.Security.Cryptography.SHA256.Create();
        var signingKeyBytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(jwtKey));

        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(signingKeyBytes),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
        };
    });

builder.Services.AddScoped<AuthService>();

var identityConn = builder.Configuration.GetConnectionString("IdentityDb");
if (!string.IsNullOrWhiteSpace(identityConn))
{
    // Use a configured server version rather than ServerVersion.AutoDetect: AutoDetect
    // opens a connection while the service container is still being built, so a briefly
    // unavailable database turns into a hard startup failure. Mirrors the version
    // resolution already used by DesignTimeDbContextFactory.
    var serverVersion = Version.TryParse(builder.Configuration["DatabaseServerVersion"], out var parsedVersion)
        ? new MySqlServerVersion(parsedVersion)
        : new MySqlServerVersion(new Version(8, 0, 29));

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseMySql(identityConn, serverVersion));
}
else
{
    if (!builder.Environment.IsProduction())
    {
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("CeylonQuest_Local"));
    }
    else
    {
        throw new InvalidOperationException("IdentityDb connection string is required in Production environment.");
    }
}

builder.Services.AddScoped<RegistrationService>();
builder.Services.AddScoped<ProviderActivationService>();
builder.Services.AddScoped<UserProfileService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<PasswordResetTokenService>();
builder.Services.AddScoped<PasswordResetService>();
builder.Services.AddScoped<AdminReportService>();

builder.Services.AddKafka(builder.Configuration);
builder.Services.AddScoped<OtpService>();
builder.Services.AddScoped<ProviderAccountActivationService>();
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService<ProviderApprovedConsumer>();
}

var app = builder.Build();

// Fail loudly at startup rather than silently at first use. These values are secrets
// or environment-specific and are supplied via environment variables / app settings
// (Email__Password, Jwt__Key), never committed to appsettings.json.
if (app.Environment.IsProduction())
{
    var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Program");

    if (string.IsNullOrWhiteSpace(builder.Configuration["Email:Password"]))
    {
        startupLogger.LogError(
            "Email:Password is not configured. Password-reset emails will fail. Set the Email__Password app setting.");
    }

    if (string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Key"]))
    {
        startupLogger.LogError(
            "Jwt:Key is not configured; falling back to the built-in development key. Set the Jwt__Key app setting.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Only redirect to HTTPS outside Development: the Vite dev proxy speaks plain
// HTTP to this service and cannot follow a 307 to the self-signed HTTPS port.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseCors(FrontendPolicy);
app.UseAuthentication();
app.UseAuthorization();

if (!app.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var disableMigrationsConfig = builder.Configuration["DisableMigrations"];
            var disableMigrationsEnv = Environment.GetEnvironmentVariable("DisableMigrations");
            var disableMigrations = string.Equals(disableMigrationsConfig, "true", StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(disableMigrationsEnv, "true", StringComparison.OrdinalIgnoreCase);

            try
            {
                var providerName = db.Database.ProviderName ?? string.Empty;
                if (!disableMigrations && !providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase))
                {
                    db.Database.Migrate();
                }
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
                logger.LogWarning(ex, "Skipping migrations because they could not be applied or were disabled.");
            }

            // The schema this block used to patch by hand at every startup is now owned
            // entirely by EF migrations:
            //   PasswordResetTokens                -> 20260826112445_AddPasswordResetToken
            //   ProviderServicePrices/TimeSlots    -> 20260824024121_AddProviderDashboardTables
            //   Users.ProfilePictureUrl, and the
            //   legacy ProviderApplications drop   -> 20260830164618_AddProfilePictureUrlAndDropLegacyProviderApplications

            try
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
                DbSeeder.SeedAdminUserAsync(db, builder.Configuration, logger).GetAwaiter().GetResult();
            }
            catch (Exception exSeed)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
                logger.LogError(exSeed, "Admin seeding failed");
            }
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
            logger.LogError(ex, "Failed to apply migrations or seeds on startup");
        }
    }
}
else
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
    logger.LogInformation("Skipping database migrations because environment is Testing");
}

app.MapControllers();

app.Run();

public partial class Program { }