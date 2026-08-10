using PassengerService.Models;
using Microsoft.EntityFrameworkCore;

namespace PassengerService.Data;

public class PassengerDbContext : DbContext
{
    public PassengerDbContext(DbContextOptions<PassengerDbContext> options) : base(options)
    {
    }

    public DbSet<Reward> Rewards { get; set; } = null!;
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

        modelBuilder.Entity<Reward>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.Points).IsRequired();
            entity.Property(e => e.TransactionType).IsRequired().HasMaxLength(50);
        });
    }
}
