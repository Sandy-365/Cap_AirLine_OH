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
    Task<IEnumerable<Flight>> SearchAsync(string source, string destination, DateTime departureDate);

    // FlightSchedule methods
    Task<FlightSchedule?> GetScheduleByIdAsync(int id);
    Task<IEnumerable<FlightSchedule>> GetSchedulesByFlightIdAsync(int flightId);
    Task<FlightSchedule> AddScheduleAsync(FlightSchedule schedule);
    Task UpdateScheduleAsync(FlightSchedule schedule);
    Task DeleteScheduleAsync(int id);
    Task<IEnumerable<FlightSchedule>> SearchSchedulesAsync(string source, string destination, DateTime departureDate);
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
    /// Searches flights by matching source, destination, and departure date.
    /// </summary>
    public async Task<IEnumerable<Flight>> SearchAsync(string source, string destination, DateTime departureDate)
    {
        return await _context.Flights
            .Where(f => f.Source == source &&
                        f.Destination == destination &&
                        f.DepartureTime.Date == departureDate.Date)
            .ToListAsync();
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
    /// Searches schedules by route and date. Excludes cancelled schedules.
    /// Uses IST time conversion for accurate comparison.
    /// </summary>
    public async Task<IEnumerable<FlightSchedule>> SearchSchedulesAsync(string source, string destination, DateTime departureDate)
    {
        return await _context.FlightSchedules
            .Include(s => s.Flight)
            .Where(s => s.Flight!.Source == source &&
                        s.Flight.Destination == destination &&
                        s.DepartureTime.Date == departureDate.Date &&
                        s.Status != FlightStatus.Cancelled)
            .ToListAsync();
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
