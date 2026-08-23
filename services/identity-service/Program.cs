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
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(signingKeyBytes)
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
builder.Services.AddScoped<ProviderApplicationService>();
builder.Services.AddScoped<ProviderActivationService>();
builder.Services.AddScoped<UserProfileService>();

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
        // Fallback: ensure ProviderApplications table exists (in case migrations weren't detected/applied)
        try
        {
            var providerName2 = db.Database.ProviderName ?? string.Empty;
            if (!providerName2.Contains("InMemory", StringComparison.OrdinalIgnoreCase))
            {
                db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS `ProviderApplications` (
                `Id` char(36) NOT NULL,
                `FirstName` longtext NOT NULL,
                `LastName` longtext NOT NULL,
                `Email` varchar(255) NOT NULL,
                `PhoneNumber` longtext NOT NULL,
                `BusinessName` longtext NOT NULL,
                `ServiceType` longtext NOT NULL,
                `Location` longtext NOT NULL,
                `Description` longtext NOT NULL,
                `LegalDocumentFileName` longtext NULL,
                `Status` int NOT NULL,
                `CreatedAt` datetime(6) NOT NULL,
                PRIMARY KEY (`Id`)
            ) CHARACTER SET = utf8mb4;");
            }
        }
        catch (Exception exSql)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
            logger.LogError(exSql, "CREATE TABLE fallback failed");
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
        logger.LogError(ex, "Failed to apply migrations on startup");
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