using BookingService.Data;
using BookingService.Models;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Repositories;

public interface IPassengerRepository
{
    Task<Passenger?> GetPassengerByIdAsync(int passengerId);
    Task<List<Passenger>> GetPassengersByBookingIdAsync(int bookingId);
    Task<Passenger?> GetPassengerByAadharAsync(string aadharCardNo);
    Task AddPassengerAsync(Passenger passenger);
    Task UpdatePassengerAsync(Passenger passenger);
    Task DeletePassengerAsync(int passengerId);
    Task<bool> IsAadharUniqueAsync(string aadharCardNo, int? excludePassengerId = null);
    Task<bool> IsAadharDuplicateInScheduleAsync(string aadharCardNo, int scheduleId, int? excludePassengerId = null);
}

public class PassengerRepository : IPassengerRepository
{
    private readonly BookingDbContext _context;

    public PassengerRepository(BookingDbContext context)
    {
        _context = context;
    }



    /// <summary>
    /// Fetches a passenger by primary key ID.
    /// </summary>
    /// <param name="passengerId"></param>
    /// <returns></returns>
    public async Task<Passenger?> GetPassengerByIdAsync(int passengerId)
    {
        return await _context.Passengers.FindAsync(passengerId);
    }






    /// <summary>
    /// Retrieves all passengers linked to a specific booking.
    /// </summary>
    /// <param name="bookingId"></param>
    /// <returns></returns>
    public async Task<List<Passenger>> GetPassengersByBookingIdAsync(int bookingId)
    {
        return await _context.Passengers
            .Where(p => p.BookingId == bookingId)
            .ToListAsync();
    }





    /// <summary>
    /// Looks up a passenger by Aadhar card number for uniqueness checks.
    /// </summary>
    /// <param name="aadharCardNo"></param>
    /// <returns></returns>
    public async Task<Passenger?> GetPassengerByAadharAsync(string aadharCardNo)
    {
        return await _context.Passengers
            .FirstOrDefaultAsync(p => p.AadharCardNo == aadharCardNo);
    }






    /// <summary>
    /// Inserts a new passenger record into the database.
    /// </summary>
    /// <param name="passenger"></param>
    /// <returns></returns>
    public async Task AddPassengerAsync(Passenger passenger)
    {
        await _context.Passengers.AddAsync(passenger);
        await _context.SaveChangesAsync();
    }






    /// <summary>
    /// Updates an existing passenger record.
    /// </summary>
    /// <param name="passenger"></param>
    /// <returns></returns>
    public async Task UpdatePassengerAsync(Passenger passenger)
    {
        _context.Passengers.Update(passenger);
        await _context.SaveChangesAsync();
    }





    /// <summary>
    /// Removes a passenger by ID. No-ops if not found.
    /// </summary>
    /// <param name="passengerId"></param>
    /// <returns></returns>
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
    ///  Checks if an Aadhar number is unique across all passengers. Optionally excludes a specific passenger ID.
    /// </summary>
    /// <param name="aadharCardNo"></param>
    /// <param name="excludePassengerId"></param>
    /// <returns></returns>
    public async Task<bool> IsAadharUniqueAsync(string aadharCardNo, int? excludePassengerId = null)
    {
        var query = from p in _context.Passengers
                    join b in _context.Bookings on p.BookingId equals b.Id
                    where p.AadharCardNo == aadharCardNo
                    && b.Status != Shared.Models.BookingStatus.Cancelled
                    && b.Status != Shared.Models.BookingStatus.PaymentFailed
                    && p.Status != Models.PassengerStatus.Cancelled
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
    /// <param name="aadharCardNo"></param>
    /// <param name="scheduleId"></param>
    /// <param name="excludePassengerId"></param>
    /// <returns></returns>
    public async Task<bool> IsAadharDuplicateInScheduleAsync(string aadharCardNo, int scheduleId, int? excludePassengerId = null)
    {
        var query = from p in _context.Passengers
                    join b in _context.Bookings on p.BookingId equals b.Id
                    where p.AadharCardNo == aadharCardNo && b.ScheduleId == scheduleId
                    && b.Status != Shared.Models.BookingStatus.Cancelled 
                    && b.Status != Shared.Models.BookingStatus.PaymentFailed
                    && p.Status != Models.PassengerStatus.Cancelled
                    select p;

        if (excludePassengerId.HasValue)
        {
            query = query.Where(p => p.Id != excludePassengerId.Value);
        }

        return await query.AnyAsync();
    }
}
