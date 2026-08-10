using BackOfficeService.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Security;

namespace BackOfficeService.Data;

public static class DbInitializer
{
    public static void Initialize(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BackOfficeDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<BackOfficeDbContext>>();

        try
        {
            context.Database.Migrate();

            var superAdminEmail = "superadmin@airline.com";
            var defaultPassword = "admin123";

            var existingSuperAdmin = context.BackofficeProfiles.FirstOrDefault(a => a.Email.ToLower() == superAdminEmail.ToLower());
            if (existingSuperAdmin == null)
            {
                logger.LogInformation("Seeding SuperAdmin account in BackofficeProfiles...");
                var superAdmin = new BackofficeProfile
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
                    RoleTitle = "System SuperAdmin",
                    CreatedAt = DateTime.UtcNow
                };

                context.BackofficeProfiles.Add(superAdmin);
                context.SaveChanges();

                logger.LogWarning("SuperAdmin account seeded successfully.");
                logger.LogWarning($"SUPERADMIN EMAIL: {superAdminEmail}");
                logger.LogWarning($"SUPERADMIN PASSWORD: {defaultPassword}");
            }
            else
            {
                // Force reset password to admin123 on startup
                existingSuperAdmin.PasswordHash = PasswordHasher.Hash(defaultPassword);
                existingSuperAdmin.IsActive = true;
                existingSuperAdmin.IsEmailVerified = true;
                context.SaveChanges();
                logger.LogInformation($"SuperAdmin password updated to '{defaultPassword}' for {superAdminEmail}.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
}
