using BookingService.DTOs;
using BookingService.Models;
using BookingService.Repositories;

namespace BookingService.Services;

public interface IPassengerService
{
    Task<PassengerResponseDto?> GetPassengerAsync(int passengerId);
    Task<List<PassengerResponseDto>> GetPassengersForBookingAsync(int bookingId);
    Task<PassengerResponseDto> CreatePassengerAsync(int bookingId, CreatePassengerDto dto);
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





    /// <summary>
    /// Retrieves a single passenger by ID. Returns null if not found with a warning log.
    /// </summary>
    /// 
    /// <param name="passengerId"></param>
    /// 
    /// <returns></returns>
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






    /// <summary>
    /// Returns all passengers associated with a specific booking ID.
    /// </summary>
    /// 
    /// <param name="bookingId"></param>
    /// 
    /// <returns></returns>
    public async Task<List<PassengerResponseDto>> GetPassengersForBookingAsync(int bookingId)
    {
        var passengers = await _passengerRepository.GetPassengersByBookingIdAsync(bookingId);
        return passengers.Select(MapToResponseDto).ToList();
    }







    /// <summary>
    /// Creates a passenger linked to a booking. 
    /// Validates booking existence and Aadhar uniqueness. Updates booking passenger counts.
    /// </summary>
    /// <param name="bookingId"></param>
    /// <param name="dto"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<PassengerResponseDto> CreatePassengerAsync(int bookingId, CreatePassengerDto dto)
    {
        // Validate booking exists
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null)
        {
            throw new InvalidOperationException($"Booking with ID {bookingId} not found");
        }

        // Validate Aadhar uniqueness
        if (!await ValidateAadharNumberAsync(dto.AadharCardNo))
        {
            throw new InvalidOperationException("This Aadhar card number is already registered");
        }

        var passenger = new Passenger
        {
            BookingId = bookingId,
            Name = dto.Name,
            Age = dto.Age,
            Gender = dto.Gender,
            AadharCardNo = dto.AadharCardNo,
            PassportNumber = dto.PassportNumber,
            Nationality = dto.Nationality,
            DietaryRequirements = dto.DietaryRequirements,
            MedicalNeeds = dto.MedicalNeeds,
            MedicalAlerts = dto.MedicalAlerts,
            SeatNumber = dto.SeatNumber,
            Status = PassengerStatus.Confirmed
        };

        await _passengerRepository.AddPassengerAsync(passenger);

        // Update booking passenger count
        booking.TotalPassengers++;
        booking.ConfirmedPassengers++;
        booking.UpdatedAt = DateTime.UtcNow;
        await _bookingRepository.UpdateAsync(booking);

        _logger.LogInformation($"Passenger {passenger.Id} created for booking {bookingId}");

        return MapToResponseDto(passenger);
    }








    /// <summary>
    /// Cancels a passenger and updates booking counters (confirmed--, cancelled++).
    /// Prevents cancellation of already-cancelled passengers.
    /// </summary>
    /// <param name="passengerId"></param>
    /// <param name="dto"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task CancelPassengerAsync(int passengerId, CancelPassengerDto dto)
    {
        var passenger = await _passengerRepository.GetPassengerByIdAsync(passengerId);

        if (passenger == null)
        {
            throw new InvalidOperationException($"Passenger with ID {passengerId} not found");
        }

        if (passenger.Status == PassengerStatus.Cancelled)
        {
            throw new InvalidOperationException("Passenger is already cancelled");
        }

        passenger.Status = PassengerStatus.Cancelled;
        passenger.CancelledAt = DateTime.UtcNow;
        passenger.CancellationReason = dto.CancellationReason;

        await _passengerRepository.UpdatePassengerAsync(passenger);

        // Update booking passenger count
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








    /// <summary>
    /// Validates Aadhar number format (12 digits) and checks uniqueness in the database. 
    /// Optionally excludes a specific passenger ID for updates.
    /// </summary>
    /// <param name="aadharCardNo"></param>
    /// <param name="excludePassengerId"></param>
    /// <returns></returns>
    public async Task<bool> ValidateAadharNumberAsync(string aadharCardNo, int? excludePassengerId = null)
    {
        // Validate 12 digit format
        if (string.IsNullOrEmpty(aadharCardNo) || !System.Text.RegularExpressions.Regex.IsMatch(aadharCardNo, @"^\d{12}$"))
        {
            return false;
        }

        // Check uniqueness in database
        return await _passengerRepository.IsAadharUniqueAsync(aadharCardNo, excludePassengerId);
    }

    private PassengerResponseDto MapToResponseDto(Passenger passenger)
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
            SeatNumber = passenger.SeatNumber,
            CancelledAt = passenger.CancelledAt,
            CancellationReason = passenger.CancellationReason,
            CreatedAt = passenger.CreatedAt
        };
    }
}

