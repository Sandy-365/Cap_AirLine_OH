using BookingService.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace BookingService.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(int id);
    Task<Booking?> GetByPNRAsync(string pnr);
    Task<Booking> AddAsync(Booking booking);
    Task UpdateAsync(Booking booking);
    Task DeleteAsync(int id);
    Task<IEnumerable<Booking>> GetByUserIdAsync(int userId);
    Task<IEnumerable<Booking>> GetByScheduleIdAsync(int scheduleId);
    Task<IEnumerable<Booking>> GetByFlightIdAsync(int flightId);
    Task<IEnumerable<string>> GetOccupiedSeatsAsync(int flightId, int? scheduleId);
    Task<IEnumerable<Booking>> GetAllAsync();
}

public class BookingRepository : IBookingRepository
{
    private readonly BookingService.Data.BookingDbContext _context;

    public BookingRepository(BookingService.Data.BookingDbContext context)
    {
        _context = context;
    }


    /// <summary>
    /// Fetches a booking by ID with eager-loaded passenger collection.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<Booking?> GetByIdAsync(int id)
    {
        return await _context.Bookings.Include(b => b.Passengers).FirstOrDefaultAsync(b => b.Id == id);
    }



    /// <summary>
    /// Looks up a booking by its PNR code with included passengers.
    /// </summary>
    /// <param name="pnr"></param>
    /// <returns></returns>
    public async Task<Booking?> GetByPNRAsync(string pnr)
    {
        return await _context.Bookings.Include(b => b.Passengers).FirstOrDefaultAsync(b => b.PNR == pnr);
    }




    /// <summary>
    /// Inserts a new booking record and returns the saved entity with generated ID.
    /// </summary>
    /// <param name="booking"></param>
    /// <returns></returns>
    public async Task<Booking> AddAsync(Booking booking)
    {
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
        return booking;
    }




    /// <summary>
    /// Updates an existing booking in the database and persists changes.
    /// </summary>
    /// <param name="booking"></param>
    /// <returns></returns>
    public async Task UpdateAsync(Booking booking)
    {
        _context.Bookings.Update(booking);
        await _context.SaveChangesAsync();
    }





    /// <summary>
    /// Removes a booking by ID. No-ops silently if booking doesn't exist.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task DeleteAsync(int id)
    {
        var booking = await GetByIdAsync(id);
        if (booking != null)
        {
            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
        }
    }





    /// <summary>
    /// Retrieves all bookings belonging to a specific user.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<IEnumerable<Booking>> GetByUserIdAsync(int userId)
    {
        return await _context.Bookings.Where(b => b.UserId == userId).ToListAsync();
    }





    /// <summary>
    /// Fetches all bookings for a flight schedule with included passengers.
    /// </summary>
    /// <param name="scheduleId"></param>
    /// <returns></returns>
    public async Task<IEnumerable<Booking>> GetByScheduleIdAsync(int scheduleId)
    {
        return await _context.Bookings.Include(b => b.Passengers).Where(b => b.ScheduleId == scheduleId).ToListAsync();
    }





    /// <summary>
    /// Retrieves all bookings for a flight ID with included passengers.
    /// </summary>
    /// <param name="flightId"></param>
    /// <returns></returns>
    public async Task<IEnumerable<Booking>> GetByFlightIdAsync(int flightId)
    {
        return await _context.Bookings.Include(b => b.Passengers).Where(b => b.FlightId == flightId).ToListAsync();
    }





    /// <summary>
    /// Returns distinct seat numbers from non-cancelled passengers in non-cancelled bookings for a flight/schedule.
    /// </summary>
    /// <param name="flightId"></param>
    /// <param name="scheduleId"></param>
    /// <returns></returns>
    public async Task<IEnumerable<string>> GetOccupiedSeatsAsync(int flightId, int? scheduleId)
    {
        var query = _context.Bookings
            .Include(b => b.Passengers)
            .Where(b => b.FlightId == flightId && b.Status != BookingStatus.Cancelled);

        if (scheduleId.HasValue)
        {
            query = query.Where(b => b.ScheduleId == scheduleId.Value);
        }

        var bookings = await query.ToListAsync();

        var occupiedSeats = bookings
            .SelectMany(b => b.Passengers)
            .Where(p => p.Status != PassengerStatus.Cancelled && !string.IsNullOrEmpty(p.SeatNumber))
            .Select(p => p.SeatNumber!)
            .Distinct()
            .ToList();

        return occupiedSeats;
    }





    /// <summary>
    /// Retrieves all bookings from the database.
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<Booking>> GetAllAsync()
    {
        return await _context.Bookings.ToListAsync();
    }
}
