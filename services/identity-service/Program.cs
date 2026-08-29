using IdentityService.Data;
using IdentityService.Services;
using Microsoft.EntityFrameworkCore;
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

// JWT Authentication
// Defer reading Jwt:Key until the authentication setup so test overrides via ConfigureAppConfiguration are respected.
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        // Read JWT settings from configuration at this point (picks up test overrides)
        var jwtKey = builder.Configuration["Jwt:Key"];
        var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CeylonQuest";
        var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CeylonQuestAudience";
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            jwtKey = "dev_secret_do_not_use_in_production_please_change_which_is_long_enough";
        }

        // Derive a 256-bit signing key from configured jwtKey to ensure minimum key size
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

// Configure the DB provider: prefer configured MySQL, otherwise use InMemory for local/dev (not for Production)
var identityConn = builder.Configuration.GetConnectionString("IdentityDb");
if (!string.IsNullOrWhiteSpace(identityConn))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseMySql(identityConn, ServerVersion.AutoDetect(identityConn)));
}
else
{
    // Only fall back to InMemory when not in Production
    if (!builder.Environment.IsProduction())
    {
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("CeylonQuest_Local"));
    }
    else
    {
        // In production require a configured connection string
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

// Kafka: consumes provider.approved from Provider/Catalog Service (Identity Service does not publish it).
builder.Services.AddKafka(builder.Configuration);
builder.Services.AddScoped<OtpService>();
builder.Services.AddScoped<ProviderAccountActivationService>();
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService<ProviderApprovedConsumer>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Apply pending EF Core migrations at startup (ensures ProviderApplications table exists)
if (!app.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // Respect an explicit DisableMigrations flag (from config or environment) for tests and CI.
        var disableMigrationsConfig = builder.Configuration["DisableMigrations"];
        var disableMigrationsEnv = Environment.GetEnvironmentVariable("DisableMigrations");
        var disableMigrations = string.Equals(disableMigrationsConfig, "true", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(disableMigrationsEnv, "true", StringComparison.OrdinalIgnoreCase);

        // Only run EF Core migrations when using a relational provider (MySQL) and migrations are not explicitly disabled.
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
        // Drop the legacy ProviderApplications table from MySQL if it exists
        try
        {
            var providerName = db.Database.ProviderName ?? string.Empty;
            if (!providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase))
            {
                db.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS `ProviderApplications`;");
            }
        }
        catch (Exception exDrop)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
            logger.LogWarning(exDrop, "Could not drop legacy ProviderApplications table.");
        }

        try
        {
            var providerName = db.Database.ProviderName ?? string.Empty;
            if (!providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase))
            {
                db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS `PasswordResetTokens` (
                    `Id` char(36) NOT NULL,
                    `UserId` char(36) NOT NULL,
                    `TokenHash` varchar(255) NOT NULL,
                    `ExpiresAt` datetime(6) NOT NULL,
                    `UsedAt` datetime(6) NULL,
                    `CreatedAt` datetime(6) NOT NULL,
                    PRIMARY KEY (`Id`),
                    KEY `IX_PasswordResetTokens_UserId` (`UserId`)
                ) CHARACTER SET = utf8mb4;");

                db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS `ProviderServicePrices` (
                    `Id` char(36) NOT NULL,
                    `ProviderId` char(36) NOT NULL,
                    `ServiceName` longtext NOT NULL,
                    `Description` longtext NOT NULL,
                    `PricePerUnit` decimal(65,30) NOT NULL,
                    `Unit` longtext NOT NULL,
                    `IsActive` tinyint(1) NOT NULL,
                    `UpdatedAt` datetime(6) NOT NULL,
                    PRIMARY KEY (`Id`)
                ) CHARACTER SET = utf8mb4;");

                db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS `ProviderTimeSlots` (
                    `Id` char(36) NOT NULL,
                    `ProviderId` char(36) NOT NULL,
                    `Date` longtext NOT NULL,
                    `StartTime` longtext NOT NULL,
                    `EndTime` longtext NOT NULL,
                    `IsAvailable` tinyint(1) NOT NULL,
                    `CreatedAt` datetime(6) NOT NULL,
                    PRIMARY KEY (`Id`)
                ) CHARACTER SET = utf8mb4;");

                db.Database.ExecuteSqlRaw("UPDATE `Users` SET `Role` = 0, `RequiresPasswordChange` = 0, `IsActive` = 1 WHERE `Email` = 'vindyawijerathna1@gmail.com';");
            }
        }
        catch { }

        // Seed Default Admin Account if not already present
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