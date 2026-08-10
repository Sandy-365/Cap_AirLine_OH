using StaffService.Models;
using Microsoft.EntityFrameworkCore;

namespace StaffService.Data;

public class StaffDbContext : DbContext
{
    public StaffDbContext(DbContextOptions<StaffDbContext> options) : base(options)
    {
    }

    public DbSet<StaffProfile> StaffProfiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<StaffProfile>().ToTable("StaffProfiles");
    }
}
