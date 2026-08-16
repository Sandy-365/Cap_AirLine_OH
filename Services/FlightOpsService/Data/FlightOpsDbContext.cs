using FlightOpsService.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace FlightOpsService.Data;

public class FlightOpsDbContext : DbContext
{
    public FlightOpsDbContext(DbContextOptions<FlightOpsDbContext> options) : base(options)
    {
    }

    // Flight domain
    public DbSet<Flight> Flights { get; set; } = null!;
    public DbSet<FlightSchedule> FlightSchedules { get; set; } = null!;

    // Booking domain
    public DbSet<Booking> Bookings { get; set; } = null!;
    public DbSet<BookingPassenger> Passengers { get; set; } = null!;

    // CheckIn domain
    public DbSet<CheckIn> CheckIns { get; set; } = null!;
    public DbSet<Baggage> Baggages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Flight ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Flight>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FlightNumber).IsUnique();
            entity.Property(e => e.FlightNumber).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Source).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Destination).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Aircraft).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Gate).HasMaxLength(10);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.EconomyPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.BusinessPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.FirstClassPrice).HasColumnType("decimal(18,2)");
        });

        // ── FlightSchedule ────────────────────────────────────────────────────
        modelBuilder.Entity<FlightSchedule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Gate).HasMaxLength(10);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.EconomyPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.BusinessPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.FirstClassPrice).HasColumnType("decimal(18,2)");

            // Foreign Key to Flight
            entity.HasOne(e => e.Flight)
                  .WithMany()
                  .HasForeignKey(e => e.FlightId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Booking ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PNR).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ScheduleId);
            entity.Property(e => e.PNR).IsRequired().HasMaxLength(10);
            entity.Property(e => e.SeatClass).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.PaymentStatus).HasConversion<string>();
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.BaggageWeight).HasColumnType("decimal(18,2)");

            // Foreign Key to Flight
            entity.HasOne(e => e.Flight)
                  .WithMany()
                  .HasForeignKey(e => e.FlightId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Foreign Key to FlightSchedule
            entity.HasOne(e => e.Schedule)
                  .WithMany()
                  .HasForeignKey(e => e.ScheduleId)
                  .OnDelete(DeleteBehavior.Restrict);

            // One-to-Many relationship with Passengers
            entity.HasMany(e => e.Passengers)
                  .WithOne(p => p.Booking)
                  .HasForeignKey(p => p.BookingId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── BookingPassenger ──────────────────────────────────────────────────
        modelBuilder.Entity<BookingPassenger>(entity =>
        {
            entity.ToTable("Passengers");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.BookingId);
            entity.HasIndex(e => e.AadharCardNo);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Gender).IsRequired().HasMaxLength(20);
            entity.Property(e => e.AadharCardNo).IsRequired().HasMaxLength(12);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.CancellationReason).HasMaxLength(500);
            entity.Property(e => e.Fare).HasColumnType("decimal(18,2)");
        });

        // ── CheckIn ───────────────────────────────────────────────────────────
        modelBuilder.Entity<CheckIn>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.BookingId);
            entity.HasIndex(e => e.PassengerId);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.PassengerId).IsRequired();
            entity.Property(e => e.SeatNumber).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Gate).HasMaxLength(10);
            entity.Property(e => e.BoardingPass).IsRequired();
            entity.Property(e => e.QRCode).IsRequired();

            // Foreign Key to Booking
            entity.HasOne(e => e.Booking)
                  .WithMany()
                  .HasForeignKey(e => e.BookingId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Foreign Key to Passenger
            entity.HasOne(e => e.Passenger)
                  .WithMany()
                  .HasForeignKey(e => e.PassengerId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Foreign Key to Flight
            entity.HasOne(e => e.Flight)
                  .WithMany()
                  .HasForeignKey(e => e.FlightId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Baggage ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Baggage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.BookingId);
            entity.HasIndex(e => e.TrackingNumber).IsUnique();
            entity.Property(e => e.Weight).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Status).HasConversion<string>();

            // Foreign Key to Booking
            entity.HasOne(e => e.Booking)
                  .WithMany()
                  .HasForeignKey(e => e.BookingId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
