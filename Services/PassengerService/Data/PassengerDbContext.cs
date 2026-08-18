using PassengerService.Models;
using Microsoft.EntityFrameworkCore;

namespace PassengerService.Data;

public class PassengerDbContext : DbContext
{
    public PassengerDbContext(DbContextOptions<PassengerDbContext> options) : base(options)
    {
    }

    public DbSet<PassengerProfile> PassengerProfiles { get; set; } = null!;
    public DbSet<SavedPassenger> SavedPassengers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PassengerProfile>()
            .HasMany(p => p.SavedPassengers)
            .WithOne(s => s.Profile)
            .HasForeignKey(s => s.PassengerProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
