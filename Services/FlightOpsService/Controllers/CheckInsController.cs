using FlightOpsService.DTOs;
using FlightOpsService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlightOpsService.Controllers;

/// <summary>
/// CheckIns Controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CheckInsController : ControllerBase
{
    private readonly ICheckInService _checkInService;

    public CheckInsController(ICheckInService checkInService)
    {
        _checkInService = checkInService;
    }

    /// <summary>
    /// Retrieves all check-in records, or filters by bookingId.
    /// [Allowed Roles: Logged-in Users]
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll([FromQuery] int? bookingId = null)
    {
        if (bookingId.HasValue)
        {
            var boardingPasses = await _checkInService.GetBoardingPassesByBookingAsync(bookingId.Value);
            return Ok(boardingPasses);
        }

        var results = await _checkInService.GetAllCheckInsAsync();
        return Ok(results);
    }

    /// <summary>
    /// Retrieves a single check-in record by ID. Returns 404 if not found.
    /// [Allowed Roles: Passenger, Admin, Staff]
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Passenger,Admin,Staff,GroundStaff")]
    public async Task<IActionResult> GetCheckIn(int id)
    {
        try
        {
            var result = await _checkInService.GetCheckInAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Performs passenger or staff counter Check-In.
    /// [Allowed Roles: Passenger, Admin, GroundStaff, Staff]
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CheckIn(
        [FromBody] OnlineCheckInDto dto,
        [FromQuery] string passengerName,
        [FromQuery] string flightNumber,
        [FromQuery] int flightId,
        [FromQuery] DateTime departureTime,
        [FromQuery] decimal fare)
    {
        try
        {
            if (!dto.UserId.HasValue || dto.UserId.Value <= 0)
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
                if (int.TryParse(userIdClaim, out var extractedUserId))
                {
                    dto.UserId = extractedUserId;
                }
            }

            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var result = await _checkInService.OnlineCheckInAsync(dto, passengerName, flightNumber, flightId, departureTime, fare, token);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

