using AdminService.Models;
using Microsoft.EntityFrameworkCore;

namespace AdminService.Data;

public class AdminDbContext : DbContext
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options)
    {
    }

    public DbSet<AdminProfile> AdminProfiles { get; set; }
    public DbSet<VisitorLog> VisitorLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<AdminProfile>().ToTable("AdminProfiles");
        modelBuilder.Entity<AdminProfile>().Property(x => x.Id).HasDefaultValueSql("NEWID()");

        modelBuilder.Entity<VisitorLog>().ToTable("VisitorLogs");
    }
}
