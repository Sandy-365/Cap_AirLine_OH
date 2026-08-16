using BackOfficeService.Models;
using Microsoft.EntityFrameworkCore;

namespace BackOfficeService.Data;

public class BackOfficeDbContext : DbContext
{
    public BackOfficeDbContext(DbContextOptions<BackOfficeDbContext> options) : base(options)
    {
    }

    public DbSet<BackofficeProfile> BackofficeProfiles { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<BackofficeProfile>(entity =>
        {
            entity.ToTable("BackofficeProfiles");
            entity.HasIndex(x => x.Email).IsUnique();
        });
    }
}
