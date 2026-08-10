using CheckInService.DTOs;
using CheckInService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CheckInService.Controllers;

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
    [HttpGet("{id}")]
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
    /// Generates digital boarding pass details for a check-in record.
    /// [Allowed Roles: Passenger, Admin, Staff]
    /// </summary>
    [HttpGet("{id}/boarding-pass")]
    [Authorize(Roles = "Passenger,Admin,Staff,GroundStaff")]
    public async Task<IActionResult> GenerateBoardingPass(int id)
    {
        try
        {
            var result = await _checkInService.GenerateBoardingPassAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Passenger self-service Online Check-In.
    /// [Allowed Roles: Passenger]
    /// </summary>
    [HttpPost("online")]
    [Authorize(Roles = "Passenger")]
    public async Task<IActionResult> OnlineCheckIn(
        [FromBody] OnlineCheckInDto dto,
        [FromQuery] string passengerName,
        [FromQuery] string flightNumber,
        [FromQuery] int flightId,
        [FromQuery] DateTime departureTime,
        [FromQuery] decimal fare)
    {
        try
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var result = await _checkInService.OnlineCheckInAsync(dto, passengerName, flightNumber, flightId, departureTime, fare, token);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Staff-initiated Check-In at airport counter.
    /// [Allowed Roles: Admin, GroundStaff, Staff]
    /// </summary>
    [HttpPost("staff")]
    [Authorize(Roles = "Admin,GroundStaff,Staff")]
    public async Task<IActionResult> StaffCheckIn([FromBody] StaffCheckInDto dto)
    {
        try
        {
            var result = await _checkInService.StaffCheckInAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
