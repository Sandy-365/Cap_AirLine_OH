using BookingService.DTOs;
using BookingService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Exceptions;

namespace BookingService.Controllers;

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
    [HttpGet("{id}")]
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
        catch (ServiceUnavailableException ex)
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
    /// Adds passengers to an existing booking.
    /// [Allowed Roles: Passenger, Dealer]
    /// </summary>
    [HttpPost("{bookingId}/passengers")]
    [Authorize(Roles = "Passenger,Dealer")]
    public async Task<IActionResult> AddPassengersToBooking(int bookingId, [FromBody] List<CreatePassengerDto> passengers)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (passengers == null || passengers.Count == 0)
                return BadRequest(new { message = "At least one passenger is required" });

            var addedPassengers = new List<PassengerResponseDto>();

            foreach (var passengerDto in passengers)
            {
                var passenger = await _passengerService.CreatePassengerAsync(bookingId, passengerDto);
                addedPassengers.Add(passenger);
            }

            return CreatedAtAction(nameof(GetBookingPassengers), new { bookingId = bookingId }, addedPassengers);
        }
        catch (DomainValidationException ex)
        {
            _logger.LogWarning($"Validation error: {ex.Message}");
            return BadRequest(new { message = ex.Message, errorCode = "VALIDATION_ERROR", propertyName = ex.PropertyName });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning($"Invalid operation: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error adding passengers: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets all passengers for a specific booking.
    /// [Allowed Roles: Passenger, Dealer, Admin, GroundStaff, Staff]
    /// </summary>
    [HttpGet("{bookingId}/passengers")]
    [Authorize(Roles = "Passenger,Dealer,Admin,GroundStaff,Staff")]
    public async Task<IActionResult> GetBookingPassengers(int bookingId)
    {
        try
        {
            var passengers = await _passengerService.GetPassengersForBookingAsync(bookingId);
            return Ok(passengers);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting passengers: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Cancels a booking.
    /// [Allowed Roles: Passenger, Dealer]
    /// </summary>
    [HttpPost("{id}/cancel")]
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
    /// Permanently deletes a booking record.
    /// [Allowed Roles: Passenger, Dealer, Admin]
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Passenger,Dealer,Admin")]
    public async Task<IActionResult> DeleteBooking(int id)
    {
        try
        {
            await _bookingService.DeleteBookingAsync(id);
            return Ok(new { message = "Booking deleted permanently" });
        }
        catch (BookingNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting booking: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Cancels a specific passenger from a booking.
    /// [Allowed Roles: Passenger, Dealer]
    /// </summary>
    [HttpPost("passengers/{passengerId}/cancel")]
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
