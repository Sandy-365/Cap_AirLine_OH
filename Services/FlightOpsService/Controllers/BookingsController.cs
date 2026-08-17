using FlightOpsService.DTOs;
using FlightOpsService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Exceptions;

namespace FlightOpsService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IPassengerService _passengerService;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(
        IBookingService bookingService,
        IPassengerService passengerService,
        ILogger<BookingsController> logger)
    {
        _bookingService = bookingService;
        _passengerService = passengerService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all bookings, or filters by PNR, userId, flightId, or scheduleId.
    /// [Allowed Roles: Public / Authorized Users]
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetBookings(
        [FromQuery] string? pnr = null,
        [FromQuery] int? userId = null,
        [FromQuery] int? flightId = null,
        [FromQuery] int? scheduleId = null)
    {
        try
        {
            if (!string.IsNullOrEmpty(pnr))
            {
                var result = await _bookingService.GetBookingByPnrAsync(pnr);
                return Ok(result);
            }
            if (userId.HasValue)
            {
                var results = await _bookingService.GetBookingHistoryAsync(userId.Value);
                return Ok(results);
            }
            if (flightId.HasValue)
            {
                var results = await _bookingService.GetBookingsByFlightIdAsync(flightId.Value);
                return Ok(results);
            }
            if (scheduleId.HasValue)
            {
                var results = await _bookingService.GetBookingsByScheduleAsync(scheduleId.Value);
                return Ok(results);
            }

            var all = await _bookingService.GetAllBookingsAsync();
            return Ok(all);
        }
        catch (PnrNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bookings");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets a booking by its ID.
    /// [Allowed Roles: Passenger, Dealer, Admin]
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Passenger,Dealer,Admin")]
    public async Task<IActionResult> GetBooking(int id)
    {
        try
        {
            var result = await _bookingService.GetBookingAsync(id);
            return Ok(result);
        }
        catch (BookingNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new booking.
    /// [Allowed Roles: Passenger, Dealer]
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Passenger,Dealer")]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Automatically populate user identity from JWT claims if not explicitly provided
        if (!dto.UserId.HasValue || dto.UserId.Value <= 0 || User.IsInRole("Passenger"))
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("userId")?.Value;

            if (int.TryParse(userIdClaim, out var extractedUserId))
            {
                dto.UserId = extractedUserId;
            }
        }

        if (string.IsNullOrWhiteSpace(dto.UserEmail))
        {
            dto.UserEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                ?? User.FindFirst("email")?.Value
                ?? "";
        }

        if (string.IsNullOrWhiteSpace(dto.UserName))
        {
            dto.UserName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                ?? User.FindFirst("name")?.Value
                ?? dto.UserEmail;
        }

        try
        {
            var result = await _bookingService.CreateBookingAsync(dto);
            return CreatedAtAction(nameof(GetBooking), new { id = result.Id }, result);
        }
        catch (SeatsNotAvailableException ex)
        {
            return BadRequest(new { 
                message = ex.Message,
                errorCode = "SEATS_NOT_AVAILABLE",
                availableSeats = ex.AvailableSeats,
                requestedSeats = ex.RequestedSeats,
                seatClass = ex.SeatClass
            });
        }
        catch (FlightNotFoundException ex)
        {
            return NotFound(new { 
                message = ex.Message,
                errorCode = "FLIGHT_NOT_FOUND",
                flightId = ex.FlightId
            });
        }
        catch (ScheduleNotFoundException ex)
        {
            return NotFound(new { 
                message = ex.Message,
                errorCode = "SCHEDULE_NOT_FOUND",
                scheduleId = ex.ScheduleId
            });
        }
        catch (DomainValidationException ex)
        {
            return BadRequest(new { 
                message = ex.Message,
                errorCode = "VALIDATION_ERROR",
                propertyName = ex.PropertyName,
                invalidValue = ex.InvalidValue
            });
        }
        catch (ServiceUnavailableException)
        {
            return StatusCode(503, new { 
                message = "Flight service temporarily unavailable",
                errorCode = "SERVICE_UNAVAILABLE"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking");
            return StatusCode(500, new { 
                message = "An unexpected error occurred",
                errorCode = "INTERNAL_SERVER_ERROR"
            });
        }
    }

    /// <summary>
    /// Cancels a booking.
    /// [Allowed Roles: Passenger, Dealer]
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = "Passenger,Dealer")]
    public async Task<IActionResult> CancelBooking(int id)
    {
        try
        {
            await _bookingService.CancelBookingAsync(id);
            return Ok(new { message = "Booking cancelled successfully" });
        }
        catch (BookingNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (BookingCancellationNotAllowedException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cancels a specific passenger from a booking.
    /// [Allowed Roles: Passenger, Dealer]
    /// </summary>
    [HttpPost("passengers/{passengerId:int}/cancel")]
    [Authorize(Roles = "Passenger,Dealer")]
    public async Task<IActionResult> CancelPassenger(int passengerId, [FromBody] CancelPassengerDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _passengerService.CancelPassengerAsync(passengerId, dto);
            return Ok(new { message = "Passenger cancelled successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning($"Invalid operation: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error cancelling passenger: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets the occupied seats for a flight schedule.
    /// [Allowed Roles: Passenger, Dealer, Admin]
    /// </summary>
    [HttpGet("occupied-seats")]
    [Authorize(Roles = "Passenger,Dealer,Admin")]
    public async Task<IActionResult> GetOccupiedSeats([FromQuery] int flightId, [FromQuery] int? scheduleId)
    {
        try
        {
            var seats = await _bookingService.GetOccupiedSeatsAsync(flightId, scheduleId);
            return Ok(seats);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting occupied seats: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
