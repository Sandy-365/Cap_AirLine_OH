using FlightOpsService.DTOs;
using FlightOpsService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlightOpsService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BaggagesController : ControllerBase
{
    private readonly IBaggageService _baggageService;

    public BaggagesController(IBaggageService baggageService)
    {
        _baggageService = baggageService;
    }

    /// <summary>
    /// Retrieves all registered baggage, or tracks by trackingNumber or bookingId.
    /// [Allowed Roles: Logged-in Users]
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll([FromQuery] int? bookingId = null, [FromQuery] string? trackingNumber = null)
    {
        if (bookingId.HasValue)
        {
            var byBooking = await _baggageService.GetByBookingIdAsync(bookingId.Value);
            return Ok(byBooking);
        }

        if (!string.IsNullOrEmpty(trackingNumber))
        {
            try
            {
                var tracked = await _baggageService.TrackBaggageAsync(trackingNumber);
                return Ok(tracked);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        var results = await _baggageService.GetAllBaggageAsync();
        return Ok(results);
    }

    /// <summary>
    /// Retrieves baggage tracking details by baggage ID.
    /// [Allowed Roles: GroundStaff, Passenger, Dealer, Staff]
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "GroundStaff,Passenger,Dealer,Staff")]
    public async Task<IActionResult> GetBaggage(int id)
    {
        try
        {
            var result = await _baggageService.GetBaggageAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Registers new checked baggage at the counter.
    /// [Allowed Roles: GroundStaff, Staff]
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "GroundStaff,Staff")]
    public async Task<IActionResult> AddBaggage([FromBody] AddBaggageDto dto)
    {
        try
        {
            var result = await _baggageService.AddBaggageAsync(dto);
            return CreatedAtAction(nameof(GetBaggage), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Updates baggage tracking status (CheckedIn, Loaded, Claimed, Lost).
    /// [Allowed Roles: GroundStaff, Staff]
    /// </summary>
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "GroundStaff,Staff")]
    public async Task<IActionResult> UpdateBaggageStatus(int id, [FromBody] UpdateBaggageStatusDto dto)
    {
        try
        {
            var result = await _baggageService.UpdateBaggageStatusAsync(id, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
