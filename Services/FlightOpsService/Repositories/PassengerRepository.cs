using FlightOpsService.Data;
using FlightOpsService.Models;
using Microsoft.EntityFrameworkCore;

namespace FlightOpsService.Repositories;

public interface IPassengerRepository
{
    Task<BookingPassenger?> GetPassengerByIdAsync(int passengerId);
    Task<List<BookingPassenger>> GetPassengersByBookingIdAsync(int bookingId);
    Task<BookingPassenger?> GetPassengerByAadharAsync(string aadharCardNo);
    Task AddPassengerAsync(BookingPassenger passenger);
    Task UpdatePassengerAsync(BookingPassenger passenger);
    Task DeletePassengerAsync(int passengerId);
    Task<bool> IsAadharUniqueAsync(string aadharCardNo, int? excludePassengerId = null);
    Task<bool> IsAadharDuplicateInScheduleAsync(string aadharCardNo, int scheduleId, int? excludePassengerId = null);
}

public class PassengerRepository : IPassengerRepository
{
    private readonly FlightOpsDbContext _context;

    public PassengerRepository(FlightOpsDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Fetches a passenger by primary key ID.
    /// </summary>
    public async Task<BookingPassenger?> GetPassengerByIdAsync(int passengerId)
    {
        return await _context.Passengers.FindAsync(passengerId);
    }

    /// <summary>
    /// Retrieves all passengers linked to a specific booking.
    /// </summary>
    public async Task<List<BookingPassenger>> GetPassengersByBookingIdAsync(int bookingId)
    {
        return await _context.Passengers
            .Where(p => p.BookingId == bookingId)
            .ToListAsync();
    }

    /// <summary>
    /// Looks up a passenger by Aadhar card number for uniqueness checks.
    /// </summary>
    public async Task<BookingPassenger?> GetPassengerByAadharAsync(string aadharCardNo)
    {
        return await _context.Passengers
            .FirstOrDefaultAsync(p => p.AadharCardNo == aadharCardNo);
    }

    /// <summary>
    /// Inserts a new passenger record into the database.
    /// </summary>
    public async Task AddPassengerAsync(BookingPassenger passenger)
    {
        await _context.Passengers.AddAsync(passenger);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Updates an existing passenger record.
    /// </summary>
    public async Task UpdatePassengerAsync(BookingPassenger passenger)
    {
        _context.Passengers.Update(passenger);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Removes a passenger by ID. No-ops if not found.
    /// </summary>
    public async Task DeletePassengerAsync(int passengerId)
    {
        var passenger = await GetPassengerByIdAsync(passengerId);
        if (passenger != null)
        {
            _context.Passengers.Remove(passenger);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Checks if an Aadhar number is unique across all passengers. Optionally excludes a specific passenger ID.
    /// </summary>
    public async Task<bool> IsAadharUniqueAsync(string aadharCardNo, int? excludePassengerId = null)
    {
        var query = from p in _context.Passengers
                    join b in _context.Bookings on p.BookingId equals b.Id
                    where p.AadharCardNo == aadharCardNo
                    && b.Status != Shared.Models.BookingStatus.Cancelled
                    && b.Status != Shared.Models.BookingStatus.PaymentFailed
                    && p.Status != BookingPassengerStatus.Cancelled
                    select p;

        if (excludePassengerId.HasValue)
        {
            query = query.Where(p => p.Id != excludePassengerId.Value);
        }

        return !await query.AnyAsync();
    }

    /// <summary>
    /// Checks if an Aadhar number already exists for any passenger on the same flight schedule via booking join.
    /// </summary>
    public async Task<bool> IsAadharDuplicateInScheduleAsync(string aadharCardNo, int scheduleId, int? excludePassengerId = null)
    {
        var query = from p in _context.Passengers
                    join b in _context.Bookings on p.BookingId equals b.Id
                    where p.AadharCardNo == aadharCardNo && b.ScheduleId == scheduleId
                    && b.Status != Shared.Models.BookingStatus.Cancelled 
                    && b.Status != Shared.Models.BookingStatus.PaymentFailed
                    && p.Status != BookingPassengerStatus.Cancelled
                    select p;

        if (excludePassengerId.HasValue)
        {
            query = query.Where(p => p.Id != excludePassengerId.Value);
        }

        return await query.AnyAsync();
    }
}
