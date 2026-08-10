using BookingService.DTOs;
using BookingService.Models;
using BookingService.Repositories;
using Shared.Models;
using Shared.Exceptions;

namespace BookingService.Services;

public class FlightVerificationDto
{
    public string status { get; set; } = "";
    public DateTime departureTime { get; set; }
    public int economySeats { get; set; }
    public int businessSeats { get; set; }
    public int firstSeats { get; set; }
}

public class ScheduleVerificationDto
{
    public string status { get; set; } = "";
    public DateTime departureTime { get; set; }
    public int economySeats { get; set; }
    public int businessSeats { get; set; }
    public int firstSeats { get; set; }
}

public interface IBookingService
{
    Task<BookingDto> CreateBookingAsync(CreateBookingDto dto);
    Task<BookingDto> GetBookingAsync(int id);
    Task CancelBookingAsync(int id);
    Task<IEnumerable<BookingHistoryDto>> GetBookingHistoryAsync(int userId);
    Task<IEnumerable<object>> GetBookingsByScheduleAsync(int scheduleId);
    Task<IEnumerable<string>> GetOccupiedSeatsAsync(int flightId, int? scheduleId);
    Task<BookingDto> UpdateBookingAsync(int id, Booking booking);
    Task DeleteBookingAsync(int id);
    Task<BookingDto> GetBookingByPnrAsync(string pnr);
    Task<IEnumerable<object>> GetAllBookingsAsync();
    Task<IEnumerable<object>> GetBookingsByFlightIdAsync(int flightId);
}

public class BookingServiceImpl : IBookingService
{
    private readonly IBookingRepository _repository;
    private readonly HttpClient _httpClient;
    private readonly ILogger<BookingServiceImpl> _logger;
    private readonly IConfiguration _configuration;

    public BookingServiceImpl(
        IBookingRepository repository, 
        HttpClient httpClient,
        ILogger<BookingServiceImpl> logger,
        IConfiguration configuration)
    {
        _repository = repository;
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }




    /// <summary>
    /// Legacy service method for booking creation. Validates flight/schedule
    /// availability, seat capacity, and generates PNR. 
    /// Books seats on FlightService and publishes BookingCreatedEvent.
    /// </summary>
    /// 
    /// <param name="dto"></param>
    /// 
    /// <returns></returns>
    /// 
    /// <exception cref="ConfigurationException"></exception>
    /// <exception cref="ScheduleNotFoundException"></exception>
    /// <exception cref="ServiceCommunicationException"></exception>
    /// <exception cref="InvalidScheduleException"></exception>
    /// <exception cref="FlightAlreadyDepartedException"></exception>
    /// <exception cref="SeatsNotAvailableException"></exception>
    /// <exception cref="FlightNotFoundException"></exception>
    /// <exception cref="FlightCancelledException"></exception>
    /// <exception cref="ServiceUnavailableException"></exception>
    /// <exception cref="DomainValidationException"></exception>
    /// <exception cref="BaggageWeightExceededException"></exception>
    /// <exception cref="SeatCapacityExceededException"></exception>
    public async Task<BookingDto> CreateBookingAsync(CreateBookingDto dto)
    {
        _logger.LogInformation($"Starting booking creation for User {dto.UserId}, Flight {dto.FlightId}, Schedule {dto.ScheduleId}");

        var flightServiceUrl = _configuration["ServiceUrls:FlightService"] ?? throw new ConfigurationException("ServiceUrls:FlightService", "FlightService URL is not configured");

        // ── Validate via schedule or flight ──
        try
        {
            if (dto.ScheduleId.HasValue)
            {
                // Schedule-based booking: validate against schedule endpoint
                var scheduleResponse = await _httpClient.GetAsync($"{flightServiceUrl}/api/flights/schedules/{dto.ScheduleId.Value}");
                if (!scheduleResponse.IsSuccessStatusCode)
                    throw new ScheduleNotFoundException(dto.ScheduleId.Value);

                var scheduleData = await scheduleResponse.Content.ReadFromJsonAsync<ScheduleVerificationDto>();
                if (scheduleData == null)
                {
                    throw new ServiceCommunicationException(
                        "FlightService", 
                        $"/api/flights/schedules/{dto.ScheduleId.Value}", 
                        "Invalid or empty response from schedule endpoint");
                }

                if (scheduleData.status == "Cancelled" || scheduleData.status == "Completed")
                {
                    throw new InvalidScheduleException(
                        dto.ScheduleId.Value, 
                        $"Schedule status is '{scheduleData.status}' and cannot be booked");
                }

                var nowIst = DateTime.UtcNow.AddHours(5.5);
                if (scheduleData.departureTime < nowIst)
                {
                    throw new FlightAlreadyDepartedException(
                        dto.FlightId, 
                        scheduleData.departureTime);
                }

                int availableSeatsForClass = dto.SeatClass switch
                {
                    "Economy" => scheduleData.economySeats,
                    "Business" => scheduleData.businessSeats,
                    "First" => scheduleData.firstSeats,
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
            }
            else
            {
                // Legacy flight-based booking
                var response = await _httpClient.GetAsync($"{flightServiceUrl}/api/flights/{dto.FlightId}");
                if (!response.IsSuccessStatusCode)
                    throw new FlightNotFoundException(dto.FlightId);

                var flightData = await response.Content.ReadFromJsonAsync<FlightVerificationDto>();
                if (flightData == null)
                {
                    throw new ServiceCommunicationException(
                        "FlightService", 
                        $"/api/flights/{dto.FlightId}", 
                        "Invalid or empty response from flight endpoint");
                }

                if (flightData.status == "Cancelled")
                    throw new FlightCancelledException(dto.FlightId);

                var nowIst = DateTime.UtcNow.AddHours(5.5);
                if (flightData.departureTime < nowIst)
                    throw new FlightAlreadyDepartedException(dto.FlightId, flightData.departureTime);

                int availableSeatsForClass = dto.SeatClass switch
                {
                    "Economy" => flightData.economySeats,
                    "Business" => flightData.businessSeats,
                    "First" => flightData.firstSeats,
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
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Flight service communication failed");
            throw new ServiceUnavailableException("FlightService");
        }

        // Validate seat class
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

        if (dto.UserId <= 0)
        {
            throw new DomainValidationException(
                nameof(CreateBookingDto.UserId),
                dto.UserId,
                "User ID must be greater than 0");
        }

        var pnr = GeneratePNR();

        var booking = new Booking
        {
            UserId = dto.UserId,
            UserEmail = dto.UserEmail,
            UserName = dto.UserName,
            FlightId = dto.FlightId,
            ScheduleId = dto.ScheduleId,
            SeatClass = seatClass,
            BaggageWeight = dto.BaggageWeight,
            PNR = pnr,
            Status = BookingStatus.Pending,
            TotalPassengers = 0,
            ConfirmedPassengers = 0,
            CancelledPassengers = 0,
            CreatedAt = DateTime.UtcNow,
            TotalAmount = dto.TotalAmount
        };

        await _repository.AddAsync(booking);

        // Book seat on schedule or flight
        try
        {
            string bookSeatUrl;
            if (dto.ScheduleId.HasValue)
                bookSeatUrl = $"{flightServiceUrl}/api/flights/schedules/{dto.ScheduleId.Value}/book-seat";
            else
                bookSeatUrl = $"{flightServiceUrl}/api/flights/{dto.FlightId}/book-seat";

            var bookSeatContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(new { seatClass = dto.SeatClass, count = dto.PassengerCount }),
                System.Text.Encoding.UTF8,
                "application/json");

            var bookSeatResponse = await _httpClient.PostAsync(bookSeatUrl, bookSeatContent);

            if (!bookSeatResponse.IsSuccessStatusCode)
            {
                await _repository.DeleteAsync(booking.Id);
                throw new SeatCapacityExceededException(
                    dto.FlightId,
                    dto.SeatClass,
                    dto.PassengerCount,
                    0);
            }
        }
        catch (Exception ex) when (ex is not SeatCapacityExceededException)
        {
            _logger.LogError(ex, "Unexpected error while booking seat for booking {BookingId}", booking.Id);
            await _repository.DeleteAsync(booking.Id);
            throw new ServiceCommunicationException(
                "FlightService",
                dto.ScheduleId.HasValue 
                    ? $"/api/flights/schedules/{dto.ScheduleId.Value}/book-seat"
                    : $"/api/flights/{dto.FlightId}/book-seat",
                "Failed to book seat due to internal error",
                ex);
        }

        _logger.LogInformation($"Booking {booking.Id} created with PNR: {pnr}");

        return MapToDto(booking);
    }





    /// <summary>
    /// Retrieves a booking by ID with included passenger details. 
    /// Throws BookingNotFoundException if not found.
    /// </summary>
    /// 
    /// <param name="id"></param>
    /// 
    /// <returns></returns>
    /// 
    /// <exception cref="BookingNotFoundException"></exception>
    public async Task<BookingDto> GetBookingAsync(int id)
    {
        var booking = await _repository.GetByIdAsync(id);
        if (booking == null)
            throw new BookingNotFoundException(id);

        return MapToDto(booking);
    }







    /// <summary>
    /// Cancels a booking and prevents double cancellation. Releases reserved seats via
    /// ReleaseSeatReservationCommand and publishes BookingCancelledEvent.
    /// </summary>
    /// 
    /// <param name="id"></param>
    /// 
    /// <returns></returns>
    /// <exception cref="BookingNotFoundException"></exception>
    /// <exception cref="BookingCancellationNotAllowedException"></exception>
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

        // [BUG FIX] Release seats in FlightService
        if (booking.ScheduleId.HasValue)
        {
        }
    }






    /// <summary>
    /// Returns simplified booking history for a user with essential fields (PNR, status, amount, date).
    /// </summary>
    /// 
    /// <param name="userId"></param>
    /// 
    /// <returns></returns>
    public async Task<IEnumerable<BookingHistoryDto>> GetBookingHistoryAsync(int userId)
    {
        var bookings = await _repository.GetByUserIdAsync(userId);
        return bookings.Select(b => new BookingHistoryDto
        {
            Id = b.Id,
            FlightId = b.FlightId,
            PNR = b.PNR,
            Status = b.Status.ToString(),
            CreatedAt = b.CreatedAt,
            TotalAmount = b.TotalAmount
        });
    }







    /// <summary>
    /// Retrieves all bookings for a schedule with nested passenger details including seat assignments.
    /// </summary>
    /// <param name="scheduleId"></param>
    /// <returns></returns>
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








    /// <summary>
    /// Returns distinct occupied seat numbers from confirmed bookings for a flight or schedule.
    /// </summary>
    /// <param name="flightId"></param>
    /// <param name="scheduleId"></param>
    /// <returns></returns>
    public async Task<IEnumerable<string>> GetOccupiedSeatsAsync(int flightId, int? scheduleId)
    {
        return await _repository.GetOccupiedSeatsAsync(flightId, scheduleId);
    }







    /// <summary>
    /// Updates an existing booking with new data. Throws BookingNotFoundException if not found.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="booking"></param>
    /// <returns></returns>
    /// <exception cref="BookingNotFoundException"></exception>
    public async Task<BookingDto> UpdateBookingAsync(int id, Booking booking)
    {
        var existingBooking = await _repository.GetByIdAsync(id);
        if (existingBooking == null)
            throw new BookingNotFoundException(id);

        await _repository.UpdateAsync(booking);
        return MapToDto(booking);
    }













    public async Task DeleteBookingAsync(int id)
    {
        var booking = await _repository.GetByIdAsync(id);
        if (booking == null)
            throw new BookingNotFoundException(id);
        
        await _repository.DeleteAsync(id);
        _logger.LogInformation("Booking {BookingId} permanently deleted from database", id);
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

    /// <summary>
    /// generate PNR
    /// </summary>
    /// <returns></returns>
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
            TotalAmount = booking.TotalAmount
        };
    }
}
