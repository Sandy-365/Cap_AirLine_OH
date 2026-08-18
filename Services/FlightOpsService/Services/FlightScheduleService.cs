using FlightOpsService.DTOs;
using FlightOpsService.Models;
using FlightOpsService.Repositories;
using Shared.Models;

namespace FlightOpsService.Services;

public interface IFlightScheduleService
{
    Task<FlightScheduleDto> CreateScheduleAsync(CreateScheduleDto dto);
    Task<FlightScheduleDto> GetScheduleAsync(int id);
    Task DeleteScheduleAsync(int id);
    Task CancelScheduleAsync(int id);
    Task<IEnumerable<FlightScheduleDto>> GetSchedulesByFlightIdAsync(int flightId);
    Task<IEnumerable<FlightScheduleDto>> SearchSchedulesAsync(string? source, string? destination, DateTime? departureDate, int? flightId = null);
    Task<IEnumerable<FlightScheduleDto>> GetAllSchedulesAsync();
    Task BookScheduleSeatAsync(int scheduleId, string seatClass, int count);
    Task ReleaseScheduleSeatAsync(int scheduleId, string seatClass, int count);
    Task MarkExpiredSchedulesCompletedAsync();
}

public class FlightScheduleService : IFlightScheduleService
{
    private readonly IFlightRepository _repository;
    private readonly ILogger<FlightScheduleService> _logger;

    public FlightScheduleService(
        IFlightRepository repository,
        ILogger<FlightScheduleService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<FlightScheduleDto> CreateScheduleAsync(CreateScheduleDto dto)
    {
        var flight = await _repository.GetByIdAsync(dto.FlightId);
        if (flight == null)
            throw new KeyNotFoundException($"Flight {dto.FlightId} not found");

        var econ = dto.EconomySeats > 0 ? dto.EconomySeats : flight.EconomySeats;
        var bus = dto.BusinessSeats > 0 ? dto.BusinessSeats : flight.BusinessSeats;
        var fst = dto.FirstSeats > 0 ? dto.FirstSeats : flight.FirstSeats;
        var total = econ + bus + fst;

        var schedule = new FlightSchedule
        {
            FlightId = dto.FlightId,
            DepartureTime = dto.DepartureTime,
            ArrivalTime = dto.ArrivalTime,
            Gate = !string.IsNullOrEmpty(dto.Gate) ? dto.Gate : flight.Gate,
            Status = FlightStatus.Scheduled,
            TotalSeats = total,
            AvailableSeats = total,
            EconomySeats = econ,
            BusinessSeats = bus,
            FirstSeats = fst,
            EconomyPrice = dto.EconomyPrice > 0 ? dto.EconomyPrice : flight.EconomyPrice,
            BusinessPrice = dto.BusinessPrice > 0 ? dto.BusinessPrice : flight.BusinessPrice,
            FirstClassPrice = dto.FirstClassPrice > 0 ? dto.FirstClassPrice : flight.FirstClassPrice,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddScheduleAsync(schedule);

        var created = await _repository.GetScheduleByIdAsync(schedule.Id);
        return MapToDto(created ?? schedule);
    }

    public async Task<FlightScheduleDto> GetScheduleAsync(int id)
    {
        var schedule = await _repository.GetScheduleByIdAsync(id);
        if (schedule == null)
            throw new KeyNotFoundException($"Schedule {id} not found");

        return MapToDto(schedule);
    }

    public async Task DeleteScheduleAsync(int id)
    {
        await _repository.DeleteScheduleAsync(id);
    }

    public async Task CancelScheduleAsync(int id)
    {
        var schedule = await _repository.GetScheduleByIdAsync(id);
        if (schedule == null)
            throw new KeyNotFoundException($"Schedule {id} not found");

        schedule.Status = FlightStatus.Cancelled;
        await _repository.UpdateScheduleAsync(schedule);
    }

    public async Task<IEnumerable<FlightScheduleDto>> GetSchedulesByFlightIdAsync(int flightId)
    {
        var schedules = await _repository.GetSchedulesByFlightIdAsync(flightId);
        return schedules.Select(MapToDto);
    }

    public async Task<IEnumerable<FlightScheduleDto>> SearchSchedulesAsync(string? source, string? destination, DateTime? departureDate, int? flightId = null)
    {
        var schedules = await _repository.SearchSchedulesAsync(source, destination, departureDate, flightId);
        return schedules.Select(MapToDto);
    }

    public async Task<IEnumerable<FlightScheduleDto>> GetAllSchedulesAsync()
    {
        var schedules = await _repository.GetAllSchedulesAsync();
        return schedules.Select(MapToDto);
    }

    public async Task BookScheduleSeatAsync(int scheduleId, string seatClass, int count)
    {
        var schedule = await _repository.GetScheduleByIdAsync(scheduleId);
        if (schedule == null)
            throw new KeyNotFoundException($"Schedule {scheduleId} not found");

        if (seatClass == "Economy")
        {
            if (schedule.EconomySeats < count)
                throw new InvalidOperationException($"Not enough Economy seats. Available: {schedule.EconomySeats}, Requested: {count}");
            schedule.EconomySeats -= count;
        }
        else if (seatClass == "Business")
        {
            if (schedule.BusinessSeats < count)
                throw new InvalidOperationException($"Not enough Business seats. Available: {schedule.BusinessSeats}, Requested: {count}");
            schedule.BusinessSeats -= count;
        }
        else if (seatClass == "First")
        {
            if (schedule.FirstSeats < count)
                throw new InvalidOperationException($"Not enough First Class seats. Available: {schedule.FirstSeats}, Requested: {count}");
            schedule.FirstSeats -= count;
        }
        else
        {
            throw new InvalidOperationException($"Invalid seat class: {seatClass}");
        }

        schedule.AvailableSeats -= count;
        await _repository.UpdateScheduleAsync(schedule);
        _logger.LogInformation($"Seat booked: Schedule {scheduleId}, Class: {seatClass}, Count: {count}");
    }

    public async Task ReleaseScheduleSeatAsync(int scheduleId, string seatClass, int count)
    {
        var schedule = await _repository.GetScheduleByIdAsync(scheduleId);
        if (schedule == null)
            throw new KeyNotFoundException($"Schedule {scheduleId} not found");

        if (seatClass == "Economy")
            schedule.EconomySeats += count;
        else if (seatClass == "Business")
            schedule.BusinessSeats += count;
        else if (seatClass == "First")
            schedule.FirstSeats += count;
        else
            throw new InvalidOperationException($"Invalid seat class: {seatClass}");

        schedule.AvailableSeats += count;
        await _repository.UpdateScheduleAsync(schedule);
        _logger.LogInformation($"Seat released: Schedule {scheduleId}, Class: {seatClass}, Count: {count}");
    }

    public async Task MarkExpiredSchedulesCompletedAsync()
    {
        var expired = await _repository.GetExpiredSchedulesAsync();
        foreach (var schedule in expired)
        {
            schedule.Status = FlightStatus.Completed;
            await _repository.UpdateScheduleAsync(schedule);
            _logger.LogInformation($"Schedule {schedule.Id} (Flight {schedule.FlightId}) marked as Completed");
        }
    }

    private FlightScheduleDto MapToDto(FlightSchedule schedule)
    {
        return new FlightScheduleDto
        {
            Id = schedule.Id,
            FlightId = schedule.FlightId,
            FlightNumber = schedule.Flight?.FlightNumber ?? string.Empty,
            Source = schedule.Flight?.Source ?? "",
            Destination = schedule.Flight?.Destination ?? "",
            Aircraft = schedule.Flight?.Aircraft ?? "",
            DepartureTime = schedule.DepartureTime,
            ArrivalTime = schedule.ArrivalTime,
            Gate = schedule.Gate,
            Status = schedule.Status.ToString(),
            TotalSeats = schedule.TotalSeats,
            AvailableSeats = schedule.AvailableSeats,
            EconomySeats = schedule.EconomySeats,
            BusinessSeats = schedule.BusinessSeats,
            FirstSeats = schedule.FirstSeats,
            EconomyPrice = schedule.EconomyPrice,
            BusinessPrice = schedule.BusinessPrice,
            FirstClassPrice = schedule.FirstClassPrice,
            CreatedAt = schedule.CreatedAt
        };
    }
}
