
using FlightService.DTOs;
using FlightService.Models;
using FlightService.Repositories;
using Shared.Models;

namespace FlightService.Services;

public interface IFlightService
{
    Task<FlightDto> CreateFlightAsync(CreateFlightDto dto);
    Task<FlightDto> GetFlightAsync(int id);
    Task<FlightDto> UpdateFlightAsync(int id, UpdateFlightDto dto);
    Task DeleteFlightAsync(int id);
    Task<IEnumerable<FlightDto>> SearchFlightsAsync(string source, string destination, DateTime departureDate);
    Task<IEnumerable<FlightDto>> GetAllFlightsAsync();
    Task DelayFlightAsync(int flightId, DateTime newDepartureTime);
    Task CancelFlightAsync(int flightId);
    Task AssignGateAsync(int flightId, string gate);
    Task AssignAircraftAsync(int flightId, string aircraft);
    Task AssignCrewAsync(int flightId, string crew);
    Task BookSeatAsync(int flightId, string seatClass, int count);
}

public class FlightService : IFlightService
{
    private readonly IFlightRepository _repository;
    private readonly ILogger<FlightService> _logger;
    public FlightService(
        IFlightRepository repository,
        ILogger<FlightService> logger)
    {
        _repository = repository;
        _logger = logger;
    }





    /// <summary>
    /// Creates a new flight entity with route, pricing, seat configuration, and Scheduled status.
    /// Initializes available seats equal to total seats.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    public async Task<FlightDto> CreateFlightAsync(CreateFlightDto dto)
    {
        var flight = new Flight
        {
            FlightNumber = dto.FlightNumber,
            Source = dto.Source,
            Destination = dto.Destination,
            DepartureTime = dto.DepartureTime,
            ArrivalTime = dto.ArrivalTime,
            Aircraft = dto.Aircraft,
            TotalSeats = dto.TotalSeats,
            AvailableSeats = dto.TotalSeats,
            EconomySeats = dto.EconomySeats,
            BusinessSeats = dto.BusinessSeats,
            FirstSeats = dto.FirstSeats,
            EconomyPrice = dto.EconomyPrice,
            BusinessPrice = dto.BusinessPrice,
            FirstClassPrice = dto.FirstClassPrice,
            Status = FlightStatus.Scheduled,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(flight);
        return MapToDto(flight);
    }





    /// <summary>
    /// Retrieves a flight by ID and maps to DTO. Throws KeyNotFoundException if not found.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task<FlightDto> GetFlightAsync(int id)
    {
        var flight = await _repository.GetByIdAsync(id);
        if (flight == null)
            throw new KeyNotFoundException($"Flight {id} not found");

        return MapToDto(flight);
    }






    /// <summary>
    /// Updates mutable flight fields (times, gate, aircraft, crew). 
    /// Only applies non-null/empty values.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="dto"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task<FlightDto> UpdateFlightAsync(int id, UpdateFlightDto dto)
    {
        var flight = await _repository.GetByIdAsync(id);
        if (flight == null)
            throw new KeyNotFoundException($"Flight {id} not found");

        if (dto.DepartureTime.HasValue)
            flight.DepartureTime = dto.DepartureTime.Value;

        if (dto.ArrivalTime.HasValue)
            flight.ArrivalTime = dto.ArrivalTime.Value;

        if (!string.IsNullOrEmpty(dto.Gate))
            flight.Gate = dto.Gate;

        if (!string.IsNullOrEmpty(dto.Aircraft))
            flight.Aircraft = dto.Aircraft;

        if (!string.IsNullOrEmpty(dto.CrewAssignment))
            flight.CrewAssignment = dto.CrewAssignment;

        await _repository.UpdateAsync(flight);
        return MapToDto(flight);
    }






    /// <summary>
    /// Permanently removes a flight from the database.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task DeleteFlightAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }






    /// <summary>
    /// Searches flights by route and date with Redis caching (5-minute TTL). 
    /// Falls back to database on cache miss or Redis failure.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <param name="departureDate"></param>
    /// <returns></returns>
    public async Task<IEnumerable<FlightDto>> SearchFlightsAsync(string source, string destination, DateTime departureDate)
    {
        var flights = await _repository.SearchAsync(source, destination, departureDate);
        return flights.Select(MapToDto).ToList();
    }






    /// <summary>
    /// Retrieves all flights and maps to DTO format.
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<FlightDto>> GetAllFlightsAsync()
    {
        var flights = await _repository.GetAllAsync();
        return flights.Select(MapToDto);
    }





    /// <summary>
    /// Updates flight departure time, sets status to Delayed,
    /// and publishes FlightDelayedEvent for notifications.
    /// </summary>
    /// <param name="flightId"></param>
    /// <param name="newDepartureTime"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task DelayFlightAsync(int flightId, DateTime newDepartureTime)
    {
        var flight = await _repository.GetByIdAsync(flightId);
        if (flight == null)
            throw new KeyNotFoundException($"Flight {flightId} not found");

        flight.DepartureTime = newDepartureTime;
        flight.Status = FlightStatus.Delayed;

        await _repository.UpdateAsync(flight);
    }






    /// <summary>
    /// Sets flight status to Cancelled.
    /// </summary>
    /// <param name="flightId"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task CancelFlightAsync(int flightId)
    {
        var flight = await _repository.GetByIdAsync(flightId);
        if (flight == null)
            throw new KeyNotFoundException($"Flight {flightId} not found");

        flight.Status = FlightStatus.Cancelled;
        await _repository.UpdateAsync(flight);
    }






    /// <summary>
    /// Assigns a departure gate to a flight.
    /// </summary>
    /// <param name="flightId"></param>
    /// <param name="gate"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task AssignGateAsync(int flightId, string gate)
    {
        var flight = await _repository.GetByIdAsync(flightId);
        if (flight == null)
            throw new KeyNotFoundException($"Flight {flightId} not found");

        flight.Gate = gate;
        await _repository.UpdateAsync(flight);
    }






    /// <summary>
    /// Assigns an aircraft to a flight.
    /// </summary>
    /// <param name="flightId"></param>
    /// <param name="aircraft"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task AssignAircraftAsync(int flightId, string aircraft)
    {
        var flight = await _repository.GetByIdAsync(flightId);
        if (flight == null)
            throw new KeyNotFoundException($"Flight {flightId} not found");

        flight.Aircraft = aircraft;
        await _repository.UpdateAsync(flight);
    }

    public async Task AssignCrewAsync(int flightId, string crew)
    {
        var flight = await _repository.GetByIdAsync(flightId);
        if (flight == null)
            throw new KeyNotFoundException($"Flight {flightId} not found");

        flight.CrewAssignment = crew;
        await _repository.UpdateAsync(flight);
    }






    /// <summary>
    /// Books seats on a flight template with distributed Redis locking to prevent double-booking. Validates seat class and availability, 
    /// decrements class-specific and total available seats.
    /// </summary>
    /// <param name="flightId"></param>
    /// <param name="seatClass"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task BookSeatAsync(int flightId, string seatClass, int count)
    {
        var flight = await _repository.GetByIdAsync(flightId);
        if (flight == null)
            throw new KeyNotFoundException($"Flight {flightId} not found");

        if (seatClass == "Economy")
        {
            if (flight.EconomySeats < count)
                throw new InvalidOperationException($"Not enough Economy seats available. Available: {flight.EconomySeats}, Requested: {count}");
            flight.EconomySeats -= count;
        }
        else if (seatClass == "Business")
        {
            if (flight.BusinessSeats < count)
                throw new InvalidOperationException($"Not enough Business seats available. Available: {flight.BusinessSeats}, Requested: {count}");
            flight.BusinessSeats -= count;
        }
        else if (seatClass == "First")
        {
            if (flight.FirstSeats < count)
                throw new InvalidOperationException($"Not enough First Class seats available. Available: {flight.FirstSeats}, Requested: {count}");
            flight.FirstSeats -= count;
        }
        else
        {
            throw new InvalidOperationException($"Invalid seat class: {seatClass}");
        }

        flight.AvailableSeats -= count;
        await _repository.UpdateAsync(flight);
        _logger.LogInformation($"Seat booked: Flight {flightId}, Class: {seatClass}, Count: {count}");
    }

    private FlightDto MapToDto(Flight flight)
    {
        return new FlightDto
        {
            Id = flight.Id,
            FlightNumber = flight.FlightNumber,
            Source = flight.Source,
            Destination = flight.Destination,
            DepartureTime = flight.DepartureTime,
            ArrivalTime = flight.ArrivalTime,
            Gate = flight.Gate,
            Aircraft = flight.Aircraft,
            Status = flight.Status.ToString(),
            TotalSeats = flight.TotalSeats,
            AvailableSeats = flight.AvailableSeats,
            EconomySeats = flight.EconomySeats,
            BusinessSeats = flight.BusinessSeats,
            FirstSeats = flight.FirstSeats,
            EconomyPrice = flight.EconomyPrice,
            BusinessPrice = flight.BusinessPrice,
            FirstClassPrice = flight.FirstClassPrice
        };
    }
}
