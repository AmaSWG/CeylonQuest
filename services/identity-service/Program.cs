using IdentityService.Data;
using IdentityService.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("IdentityDb"),
        ServerVersion.AutoDetect(
            builder.Configuration.GetConnectionString("IdentityDb")
        )
    ));

builder.Services.AddScoped<RegistrationService>();
builder.Services.AddScoped<ProviderApplicationService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Apply pending EF Core migrations at startup (ensures ProviderApplications table exists)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
        // Fallback: ensure ProviderApplications table exists (in case migrations weren't detected/applied)
        try
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

app.MapControllers();

app.Run();