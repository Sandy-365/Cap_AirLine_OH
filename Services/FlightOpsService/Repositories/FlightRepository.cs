using FlightOpsService.Data;
using FlightOpsService.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace FlightOpsService.Repositories;

public interface IFlightRepository
{
    Task<Flight?> GetByIdAsync(int id);
    Task<Flight?> GetByFlightNumberAsync(string flightNumber);
    Task<Flight> AddAsync(Flight flight);
    Task UpdateAsync(Flight flight);
    Task DeleteAsync(int id);
    Task<IEnumerable<Flight>> GetAllAsync();
    Task<IEnumerable<Flight>> SearchAsync(string? source, string? destination, DateTime? departureDate);

    // FlightSchedule methods
    Task<FlightSchedule?> GetScheduleByIdAsync(int id);
    Task<IEnumerable<FlightSchedule>> GetSchedulesByFlightIdAsync(int flightId);
    Task<FlightSchedule> AddScheduleAsync(FlightSchedule schedule);
    Task UpdateScheduleAsync(FlightSchedule schedule);
    Task DeleteScheduleAsync(int id);
    Task<IEnumerable<FlightSchedule>> SearchSchedulesAsync(string? source, string? destination, DateTime? departureDate, int? flightId = null);
    Task<IEnumerable<FlightSchedule>> GetAllSchedulesAsync();
    Task<IEnumerable<FlightSchedule>> GetExpiredSchedulesAsync();
}

public class FlightRepository : IFlightRepository
{
    private readonly FlightOpsDbContext _context;

    public FlightRepository(FlightOpsDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Fetches a flight by primary key ID.
    /// </summary>
    public async Task<Flight?> GetByIdAsync(int id)
    {
        return await _context.Flights.FindAsync(id);
    }

    /// <summary>
    /// Looks up a flight by its flight number.
    /// </summary>
    public async Task<Flight?> GetByFlightNumberAsync(string flightNumber)
    {
        return await _context.Flights.FirstOrDefaultAsync(f => f.FlightNumber == flightNumber);
    }

    /// <summary>
    /// Inserts a new flight and returns the saved entity.
    /// </summary>
    public async Task<Flight> AddAsync(Flight flight)
    {
        _context.Flights.Add(flight);
        await _context.SaveChangesAsync();
        return flight;
    }

    /// <summary>
    /// Updates an existing flight record.
    /// </summary>
    public async Task UpdateAsync(Flight flight)
    {
        _context.Flights.Update(flight);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Removes a flight by ID. No-ops if not found.
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        var flight = await GetByIdAsync(id);
        if (flight != null)
        {
            _context.Flights.Remove(flight);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Retrieves all flights from the database.
    /// </summary>
    public async Task<IEnumerable<Flight>> GetAllAsync()
    {
        return await _context.Flights.ToListAsync();
    }

    /// <summary>
    /// Flexible search for master flights by optional source, destination, and departure date filters.
    /// </summary>
    public async Task<IEnumerable<Flight>> SearchAsync(string? source, string? destination, DateTime? departureDate)
    {
        var query = _context.Flights.AsQueryable();

        if (!string.IsNullOrWhiteSpace(source))
            query = query.Where(f => f.Source.ToLower().Contains(source.Trim().ToLower()));

        if (!string.IsNullOrWhiteSpace(destination))
            query = query.Where(f => f.Destination.ToLower().Contains(destination.Trim().ToLower()));

        if (departureDate.HasValue)
            query = query.Where(f => f.DepartureTime.Date == departureDate.Value.Date);

        return await query.ToListAsync();
    }

    /// <summary>
    /// Fetches a schedule by ID with included Flight navigation property.
    /// </summary>
    public async Task<FlightSchedule?> GetScheduleByIdAsync(int id)
    {
        return await _context.FlightSchedules
            .Include(s => s.Flight)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    /// <summary>
    /// Retrieves all schedules for a specific flight with flight details.
    /// </summary>
    public async Task<IEnumerable<FlightSchedule>> GetSchedulesByFlightIdAsync(int flightId)
    {
        return await _context.FlightSchedules
            .Include(s => s.Flight)
            .Where(s => s.FlightId == flightId)
            .ToListAsync();
    }

    /// <summary>
    /// Inserts a new flight schedule record.
    /// </summary>
    public async Task<FlightSchedule> AddScheduleAsync(FlightSchedule schedule)
    {
        _context.FlightSchedules.Add(schedule);
        await _context.SaveChangesAsync();
        return schedule;
    }

    /// <summary>
    /// Updates an existing schedule record.
    /// </summary>
    public async Task UpdateScheduleAsync(FlightSchedule schedule)
    {
        _context.FlightSchedules.Update(schedule);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Removes a schedule by ID. No-ops if not found.
    /// </summary>
    public async Task DeleteScheduleAsync(int id)
    {
        var schedule = await GetScheduleByIdAsync(id);
        if (schedule != null)
        {
            _context.FlightSchedules.Remove(schedule);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Flexible search for flight schedules by route, date, and flightId. Excludes cancelled schedules.
    /// </summary>
    public async Task<IEnumerable<FlightSchedule>> SearchSchedulesAsync(string? source, string? destination, DateTime? departureDate, int? flightId = null)
    {
        var query = _context.FlightSchedules
            .Include(s => s.Flight)
            .Where(s => s.Status != FlightStatus.Cancelled);

        if (flightId.HasValue)
            query = query.Where(s => s.FlightId == flightId.Value);

        if (!string.IsNullOrWhiteSpace(source))
            query = query.Where(s => s.Flight!.Source.ToLower().Contains(source.Trim().ToLower()));

        if (!string.IsNullOrWhiteSpace(destination))
            query = query.Where(s => s.Flight!.Destination.ToLower().Contains(destination.Trim().ToLower()));

        if (departureDate.HasValue)
            query = query.Where(s => s.DepartureTime.Date == departureDate.Value.Date);

        return await query.ToListAsync();
    }

    /// <summary>
    /// Retrieves all schedules with flight navigation data.
    /// </summary>
    public async Task<IEnumerable<FlightSchedule>> GetAllSchedulesAsync()
    {
        return await _context.FlightSchedules
            .Include(s => s.Flight)
            .ToListAsync();
    }

    /// <summary>
    /// Finds schedules with past departure time that are not yet marked as Completed or Cancelled.
    /// </summary>
    public async Task<IEnumerable<FlightSchedule>> GetExpiredSchedulesAsync()
    {
        return await _context.FlightSchedules
            .Include(s => s.Flight)
            .Where(s => s.DepartureTime < DateTime.UtcNow &&
                        s.Status != FlightStatus.Completed &&
                        s.Status != FlightStatus.Cancelled)
            .ToListAsync();
    }
}
