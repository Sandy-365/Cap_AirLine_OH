using AdminService.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Security;

namespace AdminService.Data;

public static class DbInitializer
{
    public static void Initialize(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AdminDbContext>>();

        try
        {
            // Ensure database is created/migrated
            context.Database.Migrate();

            // Seed SuperAdmin if not exists
            var superAdminEmail = "superadmin@airline.com";
            if (!context.AdminProfiles.Any(a => a.Email.ToLower() == superAdminEmail.ToLower()))
            {
                logger.LogInformation("Seeding SuperAdmin account...");
                
                var defaultPassword = GenerateRandomPassword(12);

                var superAdmin = new AdminProfile
                {
                    Email = superAdminEmail,
                    Name = "Super Admin",
                    FirstName = "Super",
                    LastName = "Admin",
                    PasswordHash = PasswordHasher.Hash(defaultPassword),
                    Role = "SuperAdmin",
                    IsEmailVerified = true,
                    IsActive = true,
                    Department = "Technology",
                    CreatedAt = DateTime.UtcNow
                };

                context.AdminProfiles.Add(superAdmin);
                context.SaveChanges();
                
                logger.LogWarning("SuperAdmin account seeded successfully.");
                logger.LogWarning("=================================================");
                logger.LogWarning($"SUPERADMIN EMAIL: {superAdminEmail}");
                logger.LogWarning($"SUPERADMIN PASSWORD: {defaultPassword}");
                logger.LogWarning("PLEASE COPY THIS PASSWORD NOW AND CHANGE IT IMMEDIATELY!");
                logger.LogWarning("=================================================");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }

    private static string GenerateRandomPassword(int length)
    {
        const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(validChars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
