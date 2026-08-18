using FlightOpsService.DTOs;
using FlightOpsService.Models;
using FlightOpsService.Repositories;
using Shared.Exceptions;
using Shared.Models;

namespace FlightOpsService.Services;

public interface IBookingService
{
    Task<BookingDto> CreateBookingAsync(CreateBookingDto dto);
    Task<BookingDto> GetBookingAsync(int id);
    Task CancelBookingAsync(int id);
    Task<IEnumerable<BookingHistoryDto>> GetBookingHistoryAsync(int userId);
    Task<IEnumerable<object>> GetBookingsByScheduleAsync(int scheduleId);
    Task<IEnumerable<string>> GetOccupiedSeatsAsync(int flightId, int? scheduleId);
    Task<BookingDto> GetBookingByPnrAsync(string pnr);
    Task<IEnumerable<object>> GetAllBookingsAsync();
    Task<IEnumerable<object>> GetBookingsByFlightIdAsync(int flightId);
    Task ConfirmPaymentAsync(int bookingId, string? transactionId = null, string? paymentMethod = null);
}

public class BookingServiceImpl : IBookingService
{
    private readonly IBookingRepository _repository;
    private readonly IFlightService _flightService;
    private readonly IFlightScheduleService _scheduleService;
    private readonly ILogger<BookingServiceImpl> _logger;

    public BookingServiceImpl(
        IBookingRepository repository,
        IFlightService flightService,
        IFlightScheduleService scheduleService,
        ILogger<BookingServiceImpl> logger)
    {
        _repository = repository;
        _flightService = flightService;
        _scheduleService = scheduleService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new booking. Validates flight/schedule availability in-process
    /// and books seats directly without network calls.
    /// </summary>
    public async Task<BookingDto> CreateBookingAsync(CreateBookingDto dto)
    {
        _logger.LogInformation($"Starting booking creation for User {dto.UserId}, Flight {dto.FlightId}, Schedule {dto.ScheduleId}");

        decimal unitPrice = 0;

        // ── Validate via schedule or flight in-process ──
        if (dto.ScheduleId.HasValue)
        {
            FlightScheduleDto scheduleData;
            try
            {
                scheduleData = await _scheduleService.GetScheduleAsync(dto.ScheduleId.Value);
            }
            catch (KeyNotFoundException)
            {
                throw new ScheduleNotFoundException(dto.ScheduleId.Value);
            }

            if (scheduleData.Status == "Cancelled" || scheduleData.Status == "Completed")
            {
                throw new InvalidScheduleException(
                    dto.ScheduleId.Value, 
                    $"Schedule status is '{scheduleData.Status}' and cannot be booked");
            }

            var nowIst = DateTime.UtcNow.AddHours(5.5);
            if (scheduleData.DepartureTime < nowIst)
            {
                throw new FlightAlreadyDepartedException(
                    dto.FlightId, 
                    scheduleData.DepartureTime);
            }

            int availableSeatsForClass = dto.SeatClass switch
            {
                "Economy" => scheduleData.EconomySeats,
                "Business" => scheduleData.BusinessSeats,
                "First" => scheduleData.FirstSeats,
                _ => 0
            };
            if (availableSeatsForClass < dto.PassengerCount)
            {
                throw new SeatsNotAvailableException(
                    dto.FlightId,
                    dto.ScheduleId,
                    dto.SeatClass,
                    dto.PassengerCount,
                    availableSeatsForClass);
            }

            unitPrice = dto.SeatClass switch
            {
                "Business" => scheduleData.BusinessPrice,
                "First" => scheduleData.FirstClassPrice,
                _ => scheduleData.EconomyPrice
            };
        }
        else
        {
            FlightDto flightData;
            try
            {
                flightData = await _flightService.GetFlightAsync(dto.FlightId);
            }
            catch (KeyNotFoundException)
            {
                throw new FlightNotFoundException(dto.FlightId);
            }

            if (flightData.Status == "Cancelled")
                throw new FlightCancelledException(dto.FlightId);

            var nowIst = DateTime.UtcNow.AddHours(5.5);
            if (flightData.DepartureTime < nowIst)
                throw new FlightAlreadyDepartedException(dto.FlightId, flightData.DepartureTime);

            int availableSeatsForClass = dto.SeatClass switch
            {
                "Economy" => flightData.EconomySeats,
                "Business" => flightData.BusinessSeats,
                "First" => flightData.FirstSeats,
                _ => 0
            };
            if (availableSeatsForClass <= 0)
            {
                throw new SeatsNotAvailableException(
                    dto.FlightId,
                    null,
                    dto.SeatClass,
                    dto.PassengerCount,
                    0);
            }

            unitPrice = dto.SeatClass switch
            {
                "Business" => flightData.BusinessPrice,
                "First" => flightData.FirstClassPrice,
                _ => flightData.EconomyPrice
            };
        }

        // Validate seat class enum
        if (!Enum.TryParse<SeatClass>(dto.SeatClass, out var seatClass))
        {
            throw new DomainValidationException(
                nameof(CreateBookingDto.SeatClass),
                dto.SeatClass,
                $"Must be one of: {string.Join(", ", Enum.GetNames(typeof(SeatClass)))}");
        }

        if (dto.BaggageWeight < 0)
        {
            throw new DomainValidationException(
                nameof(CreateBookingDto.BaggageWeight),
                dto.BaggageWeight,
                "Baggage weight cannot be negative");
        }

        if (dto.BaggageWeight > 100)
        {
            throw new BaggageWeightExceededException(dto.BaggageWeight, 100);
        }

        var effectiveUserId = dto.UserId ?? 0;
        if (effectiveUserId <= 0)
        {
            throw new DomainValidationException(
                nameof(CreateBookingDto.UserId),
                effectiveUserId,
                "User ID could not be identified from the request or token.");
        }

        int effectivePassengerCount = (dto.Passengers != null && dto.Passengers.Count > 0)
            ? dto.Passengers.Count
            : Math.Max(1, dto.PassengerCount);

        decimal calculatedTotalAmount = unitPrice * effectivePassengerCount;

        var pnr = GeneratePNR();

        var passengerList = new List<BookingPassenger>();

        if (dto.Passengers != null && dto.Passengers.Count > 0)
        {
            foreach (var p in dto.Passengers)
            {
                passengerList.Add(new BookingPassenger
                {
                    Name = string.IsNullOrWhiteSpace(p.Name) ? (dto.UserName ?? "Passenger") : p.Name,
                    Age = p.Age > 0 ? p.Age : 30,
                    Gender = string.IsNullOrWhiteSpace(p.Gender) ? "Male" : p.Gender,
                    AadharCardNo = p.AadharCardNo ?? "",
                    PassportNumber = p.PassportNumber ?? "",
                    Nationality = string.IsNullOrWhiteSpace(p.Nationality) ? "Indian" : p.Nationality,
                    DietaryRequirements = p.DietaryRequirements ?? "Standard",
                    MedicalNeeds = p.MedicalNeeds ?? "None",
                    Status = BookingPassengerStatus.Confirmed,
                    Fare = unitPrice,
                    SeatNumber = p.SeatNumber,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        else
        {
            for (int i = 0; i < effectivePassengerCount; i++)
            {
                passengerList.Add(new BookingPassenger
                {
                    Name = i == 0 ? (dto.UserName ?? "Primary Passenger") : $"Passenger {i + 1}",
                    Age = 30,
                    Gender = "Male",
                    AadharCardNo = "",
                    PassportNumber = "",
                    Nationality = "Indian",
                    DietaryRequirements = "Standard",
                    MedicalNeeds = "None",
                    Status = BookingPassengerStatus.Confirmed,
                    Fare = unitPrice,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        var booking = new Booking
        {
            UserId = effectiveUserId,
            UserEmail = dto.UserEmail ?? "",
            UserName = dto.UserName ?? "",
            FlightId = dto.FlightId,
            ScheduleId = dto.ScheduleId,
            SeatClass = seatClass,
            BaggageWeight = dto.BaggageWeight,
            PNR = pnr,
            Status = BookingStatus.Pending,
            TotalPassengers = effectivePassengerCount,
            ConfirmedPassengers = effectivePassengerCount,
            CancelledPassengers = 0,
            CreatedAt = DateTime.UtcNow,
            TotalAmount = calculatedTotalAmount,
            Passengers = passengerList
        };

        await _repository.AddAsync(booking);

        // Book seat on schedule or flight in-process
        try
        {
            if (dto.ScheduleId.HasValue)
            {
                await _scheduleService.BookScheduleSeatAsync(dto.ScheduleId.Value, dto.SeatClass, effectivePassengerCount);
            }
            else
            {
                await _flightService.BookSeatAsync(dto.FlightId, dto.SeatClass, effectivePassengerCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to book seat for booking {BookingId}", booking.Id);
            await _repository.DeleteAsync(booking.Id);
            throw new SeatCapacityExceededException(
                dto.FlightId,
                dto.SeatClass,
                effectivePassengerCount,
                0);
        }

        _logger.LogInformation($"Booking {booking.Id} created with PNR: {pnr}");

        return MapToDto(booking);
    }

    public async Task<BookingDto> GetBookingAsync(int id)
    {
        var booking = await _repository.GetByIdAsync(id);
        if (booking == null)
            throw new BookingNotFoundException(id);

        return MapToDto(booking);
    }

    public async Task CancelBookingAsync(int id)
    {
        var booking = await _repository.GetByIdAsync(id);
        if (booking == null)
            throw new BookingNotFoundException(id);

        if (booking.Status == BookingStatus.Cancelled)
        {
            throw new BookingCancellationNotAllowedException(
                id, 
                "Booking is already cancelled");
        }

        booking.Status = BookingStatus.Cancelled;
        await _repository.UpdateAsync(booking);

        // Release seats directly in-process
        if (booking.ScheduleId.HasValue && booking.TotalPassengers > 0)
        {
            try
            {
                await _scheduleService.ReleaseScheduleSeatAsync(
                    booking.ScheduleId.Value, 
                    booking.SeatClass.ToString(), 
                    booking.TotalPassengers);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not release seats for cancelled booking {BookingId}", id);
            }
        }
    }

    public async Task<IEnumerable<BookingHistoryDto>> GetBookingHistoryAsync(int userId)
    {
        var bookings = await _repository.GetByUserIdAsync(userId);
        return bookings.Select(b => new BookingHistoryDto
        {
            Id = b.Id,
            FlightId = b.FlightId,
            ScheduleId = b.ScheduleId,
            PNR = b.PNR,
            Status = b.Status.ToString(),
            CreatedAt = b.CreatedAt,
            TotalAmount = b.TotalAmount
        });
    }

    public async Task<IEnumerable<object>> GetBookingsByScheduleAsync(int scheduleId)
    {
        var bookings = await _repository.GetByScheduleIdAsync(scheduleId);
        return bookings.Select(b => (object)new {
            Id = b.Id,
            PNR = b.PNR,
            UserId = b.UserId,
            SeatClass = b.SeatClass.ToString(),
            Status = b.Status.ToString(),
            PaymentStatus = b.PaymentStatus.ToString(),
            Passengers = b.Passengers?.Select(p => (object)new {
                p.Id,
                p.Name,
                p.Age,
                p.Gender,
                Status = p.Status.ToString(),
                Seat = p.SeatNumber ?? "TBD"
            }).ToList() ?? new List<object>()
        }).ToList();
    }

    public async Task<IEnumerable<string>> GetOccupiedSeatsAsync(int flightId, int? scheduleId)
    {
        return await _repository.GetOccupiedSeatsAsync(flightId, scheduleId);
    }

    public async Task<BookingDto> GetBookingByPnrAsync(string pnr)
    {
        var booking = await _repository.GetByPNRAsync(pnr);
        if (booking == null)
            throw new PnrNotFoundException(pnr);

        return MapToDto(booking);
    }

    public async Task<IEnumerable<object>> GetAllBookingsAsync()
    {
        var bookings = await _repository.GetAllAsync();
        return bookings.Select(b => (object)new {
            Id = b.Id,
            PNR = b.PNR,
            UserId = b.UserId,
            FlightId = b.FlightId,
            ScheduleId = b.ScheduleId,
            SeatClass = b.SeatClass.ToString(),
            Status = b.Status.ToString(),
            PaymentStatus = b.PaymentStatus.ToString(),
            TotalPassengers = b.TotalPassengers
        }).ToList();
    }

    public async Task<IEnumerable<object>> GetBookingsByFlightIdAsync(int flightId)
    {
        var bookings = await _repository.GetByFlightIdAsync(flightId);
        return bookings.Select(b => (object)new {
            Id = b.Id,
            PNR = b.PNR,
            UserId = b.UserId,
            SeatClass = b.SeatClass.ToString(),
            Status = b.Status.ToString(),
            PaymentStatus = b.PaymentStatus.ToString(),
            Passengers = b.Passengers?.Select(p => (object)new {
                p.Id,
                p.Name,
                p.Age,
                p.Gender,
                Status = p.Status.ToString(),
                Seat = p.SeatNumber ?? "TBD"
            }).ToList() ?? new List<object>()
        }).ToList();
    }

    private string GeneratePNR()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Range(0, 6).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
    }

    private BookingDto MapToDto(Booking booking)
    {
        return new BookingDto
        {
            Id = booking.Id,
            UserId = booking.UserId,
            FlightId = booking.FlightId,
            ScheduleId = booking.ScheduleId,
            SeatClass = booking.SeatClass.ToString(),
            BaggageWeight = booking.BaggageWeight,
            PNR = booking.PNR,
            Status = booking.Status.ToString(),
            PaymentStatus = booking.PaymentStatus.ToString(),
            TotalPassengers = booking.TotalPassengers,
            ConfirmedPassengers = booking.ConfirmedPassengers,
            CancelledPassengers = booking.CancelledPassengers,
            CreatedAt = booking.CreatedAt,
            TotalAmount = booking.TotalAmount,
            Passengers = booking.Passengers?.Select(p => new PassengerResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                Age = p.Age,
                Gender = p.Gender,
                AadharCardNo = p.AadharCardNo,
                PassportNumber = p.PassportNumber,
                Nationality = p.Nationality,
                DietaryRequirements = p.DietaryRequirements,
                MedicalNeeds = p.MedicalNeeds,
                MedicalAlerts = p.MedicalAlerts,
                Status = p.Status.ToString(),
                Fare = p.Fare,
                CancelledAt = p.CancelledAt,
                CancellationReason = p.CancellationReason,
                SeatNumber = p.SeatNumber,
                CreatedAt = p.CreatedAt
            }).ToList() ?? new List<PassengerResponseDto>()
        };
    }

    public async Task ConfirmPaymentAsync(int bookingId, string? transactionId = null, string? paymentMethod = null)
    {
        var booking = await _repository.GetByIdAsync(bookingId);
        if (booking == null)
            throw new BookingNotFoundException(bookingId);

        booking.Status = BookingStatus.Confirmed;
        booking.PaymentStatus = PaymentStatus.Success;
        await _repository.UpdateAsync(booking);
        _logger.LogInformation("Booking {BookingId} confirmed with status Confirmed and payment status Success", bookingId);
    }
}
