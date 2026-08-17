using FlightOpsService.DTOs;
using FlightOpsService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlightOpsService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FlightsController : ControllerBase
{
    private readonly IFlightService _flightService;
    private readonly IFlightScheduleService _scheduleService;

    public FlightsController(IFlightService flightService, IFlightScheduleService scheduleService)
    {
        _flightService = flightService;
        _scheduleService = scheduleService;
    }

    // ─── Flight Endpoints ───

    /// <summary>
    /// Retrieves all flights or searches flights if source, destination, or date are provided.
    /// [Allowed Roles: Public (None required)]
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllFlights([FromQuery] string? source = null, [FromQuery] string? destination = null, [FromQuery] string? departureDate = null)
    {
        DateTime? parsedDate = null;
        if (!string.IsNullOrWhiteSpace(departureDate))
        {
            if (DateTime.TryParse(departureDate, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d))
            {
                parsedDate = d;
            }
            else
            {
                return BadRequest(new { message = "Invalid departureDate format. Please use ISO-8601 (YYYY-MM-DD)." });
            }
        }

        if (!string.IsNullOrWhiteSpace(source) || !string.IsNullOrWhiteSpace(destination) || parsedDate.HasValue)
        {
            var searchResults = await _flightService.SearchFlightsAsync(source, destination, parsedDate);
            return Ok(searchResults);
        }

        var results = await _flightService.GetAllFlightsAsync();
        return Ok(results);
    }

    /// <summary>
    /// Retrieves a single flight by ID. Returns 404 if not found.
    /// [Allowed Roles: Public (None required)]
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetFlight(int id)
    {
        try
        {
            var result = await _flightService.GetFlightAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new flight with route, schedule, pricing, and seat configuration. Sets initial status to Scheduled.
    /// [Allowed Roles: Admin, SuperAdmin]
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> CreateFlight([FromBody] CreateFlightDto dto)
    {
        try
        {
            var result = await _flightService.CreateFlightAsync(dto);
            return CreatedAtAction(nameof(GetFlight), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Updates flight details (departure/arrival times, gate, aircraft, crew). Only updates non-null fields.
    /// [Allowed Roles: Admin, SuperAdmin]
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> UpdateFlight(int id, [FromBody] UpdateFlightDto dto)
    {
        try
        {
            var result = await _flightService.UpdateFlightAsync(id, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Permanently removes a flight from the system.
    /// [Allowed Roles: Admin, SuperAdmin]
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeleteFlight(int id)
    {
        try
        {
            await _flightService.DeleteFlightAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // ─── Schedule Endpoints ───

    /// <summary>
    /// Retrieves all flight schedules, or searches schedules if source, destination, date, or flightId are provided.
    /// [Allowed Roles: Public (None required)]
    /// </summary>
    [HttpGet("schedules")]
    public async Task<IActionResult> GetAllSchedules([FromQuery] string? source = null, [FromQuery] string? destination = null, [FromQuery] string? departureDate = null, [FromQuery] int? flightId = null)
    {
        DateTime? parsedDate = null;
        if (!string.IsNullOrWhiteSpace(departureDate))
        {
            if (DateTime.TryParse(departureDate, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d))
            {
                parsedDate = d;
            }
            else
            {
                return BadRequest(new { message = "Invalid departureDate format. Please use ISO-8601 (YYYY-MM-DD)." });
            }
        }

        if (!string.IsNullOrWhiteSpace(source) || !string.IsNullOrWhiteSpace(destination) || parsedDate.HasValue || flightId.HasValue)
        {
            var searchResults = await _scheduleService.SearchSchedulesAsync(source, destination, parsedDate, flightId);
            return Ok(searchResults);
        }

        var results = await _scheduleService.GetAllSchedulesAsync();
        return Ok(results);
    }

    /// <summary>
    /// Creates a new flight schedule instance from a flight template.
    /// [Allowed Roles: Admin, SuperAdmin, Staff]
    /// </summary>
    [HttpPost("schedules")]
    [Authorize(Roles = "Admin,SuperAdmin,Staff")]
    public async Task<IActionResult> CreateSchedule([FromBody] CreateScheduleDto dto)
    {
        try
        {
            var result = await _scheduleService.CreateScheduleAsync(dto);
            return CreatedAtAction(nameof(GetAllSchedules), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
