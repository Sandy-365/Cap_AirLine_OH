using BackOfficeService.Data;
using BackOfficeService.DTOs;
using BackOfficeService.Models;
using BackOfficeService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Security;

namespace BackOfficeService.Controllers;

/// <summary>
/// Common Backoffice Management Controller for Admin &amp; Staff users.
/// </summary>
[ApiController]
[Route("api/backoffice")]
public class BackofficeController : ControllerBase
{
    private readonly IBackofficeService _backofficeService;
    private readonly IBackofficeAuthService _authService;
    private readonly BackOfficeDbContext _context;

    public BackofficeController(
        IBackofficeService backofficeService,
        IBackofficeAuthService authService,
        BackOfficeDbContext context)
    {
        _backofficeService = backofficeService;
        _authService = authService;
        _context = context;
    }

    // ─── Dashboard & Reports ───

    /// <summary>
    /// Aggregates dashboard metrics (total bookings, revenue, active flights, total users).
    /// [Allowed Roles: SuperAdmin, Admin, HR, FinancialAdmin]
    /// </summary>
    [HttpGet("dashboard")]
    [Authorize(Roles = "SuperAdmin,Admin,HR,FinancialAdmin")]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await _backofficeService.GetDashboardAsync();
        return Ok(result);
    }

    /// <summary>
    /// Fetches booking data from FlightOpsService and filters results by date range.
    /// [Allowed Roles: SuperAdmin, Admin, HR, FinancialAdmin]
    /// </summary>
    [HttpGet("booking-report")]
    [Authorize(Roles = "SuperAdmin,Admin,HR,FinancialAdmin")]
    public async Task<IActionResult> GetBookingReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var result = await _backofficeService.GetBookingReportAsync(startDate, endDate);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves confirmed bookings from FlightOpsService, groups by date, and calculates daily revenue.
    /// [Allowed Roles: SuperAdmin, Admin, HR, FinancialAdmin]
    /// </summary>
    [HttpGet("revenue-report")]
    [Authorize(Roles = "SuperAdmin,Admin,HR,FinancialAdmin")]
    public async Task<IActionResult> GetRevenueReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var result = await _backofficeService.GetRevenueReportAsync(startDate, endDate);
        return Ok(result);
    }

    // ─── User Provisioning & Account Management ───

    /// <summary>
    /// Provision / register a new user account (Admin, HR, Staff, Dealer, etc.).
    /// [Allowed Roles: SuperAdmin, Admin, HR]
    /// </summary>
    [HttpPost("register")]
    [HttpPost("users")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    public async Task<IActionResult> ProvisionUser([FromBody] BackofficeRegisterDto dto)
    {
        try
        {
            dto.ProvisionedByAdmin = true;
            await _authService.RegisterAsync(dto);
            return Ok(new { message = "User registered successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves all backoffice users, optionally filtered by roles.
    /// [Allowed Roles: SuperAdmin, Admin, HR]
    /// </summary>
    [HttpGet("users")]
    [Authorize(Roles = "SuperAdmin,Admin,HR")]
    public async Task<IActionResult> GetUsers([FromQuery] string? roles)
    {
        var roleList = roles?.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return Ok(await _authService.GetAllUsersAsync(roleList));
    }

    /// <summary>
    /// Retrieves a backoffice user profile by ID. Returns 404 if not found.
    /// [Allowed Roles: SuperAdmin (any user), Admin / HR / Staff (own profile)]
    /// </summary>
    [HttpGet("users/{userId:int}")]
    [Authorize]
    public async Task<IActionResult> GetUser(int userId)
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId != userId.ToString() && !User.IsInRole("SuperAdmin") && !User.IsInRole("Admin") && !User.IsInRole("HR")) return Forbid();

        var user = await _authService.GetUserAsync(userId);
        if (user == null) return NotFound(new { message = "User not found" });
        return Ok(user);
    }

    /// <summary>
    /// Updates a backoffice user's profile information.
    /// [Allowed Roles: SuperAdmin, Admin, HR, or account owner]
    /// </summary>
    [HttpPut("users/{userId:int}/profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(int userId, [FromBody] BackofficeUpdateProfileDto dto)
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId != userId.ToString() && !User.IsInRole("SuperAdmin") && !User.IsInRole("Admin") && !User.IsInRole("HR")) return Forbid();

        return Ok(await _authService.UpdateProfileAsync(userId, dto));
    }

    /// <summary>
    /// Activates or deactivates a backoffice user account.
    /// [Allowed Roles: SuperAdmin, HR]
    /// </summary>
    [HttpPut("users/{userId:int}/status")]
    [Authorize(Roles = "SuperAdmin,HR")]
    public async Task<IActionResult> UpdateUserStatus(int userId, [FromBody] BackofficeUpdateStatusDto dto)
    {
        await _authService.UpdateUserStatusAsync(userId, dto.IsActive);
        return Ok(new { message = "Status updated" });
    }

    /// <summary>
    /// Permanently deletes a backoffice user profile.
    /// [Allowed Roles: SuperAdmin Only]
    /// </summary>
    [HttpDelete("users/{userId:int}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        await _authService.DeleteUserAsync(userId);
        return Ok(new { message = "User deleted" });
    }
}
