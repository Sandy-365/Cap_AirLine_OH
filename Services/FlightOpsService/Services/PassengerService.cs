using FlightOpsService.DTOs;
using FlightOpsService.Models;
using FlightOpsService.Repositories;

namespace FlightOpsService.Services;

public interface IPassengerService
{
    Task<PassengerResponseDto?> GetPassengerAsync(int passengerId);
    Task CancelPassengerAsync(int passengerId, CancelPassengerDto dto);
    Task<bool> ValidateAadharNumberAsync(string aadharCardNo, int? excludePassengerId = null);
}

public class PassengerService : IPassengerService
{
    private readonly IPassengerRepository _passengerRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<PassengerService> _logger;

    public PassengerService(
        IPassengerRepository passengerRepository,
        IBookingRepository bookingRepository,
        ILogger<PassengerService> logger)
    {
        _passengerRepository = passengerRepository;
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    public async Task<PassengerResponseDto?> GetPassengerAsync(int passengerId)
    {
        var passenger = await _passengerRepository.GetPassengerByIdAsync(passengerId);

        if (passenger == null)
        {
            _logger.LogWarning($"Passenger with ID {passengerId} not found");
            return null;
        }

        return MapToResponseDto(passenger);
    }

    public async Task CancelPassengerAsync(int passengerId, CancelPassengerDto dto)
    {
        var passenger = await _passengerRepository.GetPassengerByIdAsync(passengerId);

        if (passenger == null)
        {
            throw new InvalidOperationException($"Passenger with ID {passengerId} not found");
        }

        if (passenger.Status == BookingPassengerStatus.Cancelled)
        {
            throw new InvalidOperationException("Passenger is already cancelled");
        }

        passenger.Status = BookingPassengerStatus.Cancelled;
        passenger.CancelledAt = DateTime.UtcNow;
        passenger.CancellationReason = dto.CancellationReason;

        await _passengerRepository.UpdatePassengerAsync(passenger);

        var booking = await _bookingRepository.GetByIdAsync(passenger.BookingId);
        if (booking != null)
        {
            booking.CancelledPassengers++;
            booking.ConfirmedPassengers--;
            booking.UpdatedAt = DateTime.UtcNow;
            await _bookingRepository.UpdateAsync(booking);
        }

        _logger.LogInformation($"Passenger {passengerId} cancelled. Reason: {dto.CancellationReason}");
    }

    public async Task<bool> ValidateAadharNumberAsync(string aadharCardNo, int? excludePassengerId = null)
    {
        if (string.IsNullOrEmpty(aadharCardNo) || !System.Text.RegularExpressions.Regex.IsMatch(aadharCardNo, @"^\d{12}$"))
        {
            return false;
        }

        return await _passengerRepository.IsAadharUniqueAsync(aadharCardNo, excludePassengerId);
    }

    private PassengerResponseDto MapToResponseDto(BookingPassenger passenger)
    {
        return new PassengerResponseDto
        {
            Id = passenger.Id,
            Name = passenger.Name,
            Age = passenger.Age,
            Gender = passenger.Gender,
            AadharCardNo = passenger.AadharCardNo,
            PassportNumber = passenger.PassportNumber,
            Nationality = passenger.Nationality,
            DietaryRequirements = passenger.DietaryRequirements,
            MedicalNeeds = passenger.MedicalNeeds,
            MedicalAlerts = passenger.MedicalAlerts,
            Status = passenger.Status.ToString(),
            Fare = passenger.Fare,
            SeatNumber = passenger.SeatNumber,
            CancelledAt = passenger.CancelledAt,
            CancellationReason = passenger.CancellationReason,
            CreatedAt = passenger.CreatedAt
        };
    }
}
