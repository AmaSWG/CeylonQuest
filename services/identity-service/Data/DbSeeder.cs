using System;
using System.Threading.Tasks;
using IdentityService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IdentityService.Data;

public static class DbSeeder
{
    public const string DefaultAdminEmail = "admin@ceylonquest.com";
    public const string DefaultAdminPassword = "AdminPassword123!";

    public static async Task SeedAdminUserAsync(
        ApplicationDbContext db,
        IConfiguration configuration,
        ILogger logger)
    {
        try
        {
            var adminEmail = configuration["AdminSeed:Email"] ?? DefaultAdminEmail;
            var adminPassword = configuration["AdminSeed:Password"] ?? DefaultAdminPassword;
            var adminEmailLower = adminEmail.Trim().ToLower();

            var existingAdmin = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == adminEmailLower);
            if (existingAdmin is null)
            {
                var adminUser = new User
                {
                    Id = Guid.NewGuid(),
                    FirstName = "CeylonQuest",
                    LastName = "Admin",
                    Email = adminEmail.Trim(),
                    PhoneNumber = "+94 11 234 5678",
                    Nationality = "Sri Lankan",
                    Role = UserRole.Admin,
                    IsActive = true,
                    RequiresPasswordChange = false,
                    CreatedAt = DateTime.UtcNow
                };

                var hasher = new PasswordHasher<User>();
                adminUser.PasswordHash = hasher.HashPassword(adminUser, adminPassword);

                db.Users.Add(adminUser);
                await db.SaveChangesAsync();

                logger.LogInformation("Seeded default Admin user: {Email}", adminEmail);
            }
            else
            {
                // Ensure existing user has Admin role and is active
                if (existingAdmin.Role != UserRole.Admin || !existingAdmin.IsActive)
                {
                    existingAdmin.Role = UserRole.Admin;
                    existingAdmin.IsActive = true;
                    await db.SaveChangesAsync();
                    logger.LogInformation("Updated existing user {Email} to active Admin role", adminEmail);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed default admin user");
        }
    }
}
