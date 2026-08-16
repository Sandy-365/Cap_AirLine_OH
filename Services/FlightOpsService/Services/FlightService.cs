using FlightOpsService.DTOs;
using FlightOpsService.Models;
using FlightOpsService.Repositories;
using Shared.Models;

namespace FlightOpsService.Services;

public interface IFlightService
{
    Task<FlightDto> CreateFlightAsync(CreateFlightDto dto);
    Task<FlightDto> GetFlightAsync(int id);
    Task<FlightDto> UpdateFlightAsync(int id, UpdateFlightDto dto);
    Task DeleteFlightAsync(int id);
    Task<IEnumerable<FlightDto>> SearchFlightsAsync(string? source, string? destination, DateTime? departureDate);
    Task<IEnumerable<FlightDto>> GetAllFlightsAsync();
    Task DelayFlightAsync(int flightId, DateTime newDepartureTime);
    Task CancelFlightAsync(int flightId);
    Task AssignGateAsync(int flightId, string gate);
    Task AssignAircraftAsync(int flightId, string aircraft);
    Task AssignCrewAsync(int flightId, string crew);
    Task BookSeatAsync(int flightId, string seatClass, int count);
}

public class FlightServiceImpl : IFlightService
{
    private readonly IFlightRepository _repository;
    private readonly ILogger<FlightServiceImpl> _logger;

    public FlightServiceImpl(
        IFlightRepository repository,
        ILogger<FlightServiceImpl> logger)
    {
        _repository = repository;
        _logger = logger;
    }

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

    public async Task<FlightDto> GetFlightAsync(int id)
    {
        var flight = await _repository.GetByIdAsync(id);
        if (flight == null)
            throw new KeyNotFoundException($"Flight {id} not found");

        return MapToDto(flight);
    }

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

    public async Task DeleteFlightAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }

    public async Task<IEnumerable<FlightDto>> SearchFlightsAsync(string? source, string? destination, DateTime? departureDate)
    {
        var flights = await _repository.SearchAsync(source, destination, departureDate);
        return flights.Select(MapToDto).ToList();
    }

    public async Task<IEnumerable<FlightDto>> GetAllFlightsAsync()
    {
        var flights = await _repository.GetAllAsync();
        return flights.Select(MapToDto);
    }

    public async Task DelayFlightAsync(int flightId, DateTime newDepartureTime)
    {
        var flight = await _repository.GetByIdAsync(flightId);
        if (flight == null)
            throw new KeyNotFoundException($"Flight {flightId} not found");

        flight.DepartureTime = newDepartureTime;
        flight.Status = FlightStatus.Delayed;

        await _repository.UpdateAsync(flight);
    }

    public async Task CancelFlightAsync(int flightId)
    {
        var flight = await _repository.GetByIdAsync(flightId);
        if (flight == null)
            throw new KeyNotFoundException($"Flight {flightId} not found");

        flight.Status = FlightStatus.Cancelled;
        await _repository.UpdateAsync(flight);
    }

    public async Task AssignGateAsync(int flightId, string gate)
    {
        var flight = await _repository.GetByIdAsync(flightId);
        if (flight == null)
            throw new KeyNotFoundException($"Flight {flightId} not found");

        flight.Gate = gate;
        await _repository.UpdateAsync(flight);
    }

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
