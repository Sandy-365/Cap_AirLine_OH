using AdminService.Services;
using AdminService.Interfaces;
using AdminService.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminService.Controllers;

/// <summary>
/// Admin controller for admin operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,Admin,HR,FinancialAdmin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IAdminAuthService _authService;

    public AdminController(IAdminService adminService, IAdminAuthService authService)
    {
        _adminService = adminService;
        _authService = authService;
    }

    /// <summary>
    /// Handles admin user registration. Creates a new admin profile.
    /// [Allowed Roles: SuperAdmin Only]
    /// </summary>
    [HttpPost("register")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Register([FromBody] AdminRegisterDto dto)
    {
        try
        {
            dto.ProvisionedByAdmin = true;
            await _authService.RegisterAsync(dto);
            return Ok(new { message = "Admin registered successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Aggregates dashboard metrics (total bookings, revenue, active flights, total users).
    /// [Allowed Roles: SuperAdmin, Admin, HR, FinancialAdmin]
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await _adminService.GetDashboardAsync();
        return Ok(result);
    }

    /// <summary>
    /// Fetches booking data from the BookingService and filters results by the specified date range.
    /// [Allowed Roles: SuperAdmin, Admin, HR, FinancialAdmin]
    /// </summary>
    [HttpGet("booking-report")]
    public async Task<IActionResult> GetBookingReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var result = await _adminService.GetBookingReportAsync(startDate, endDate);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves confirmed bookings from BookingService, groups them by date, and calculates daily revenue.
    /// [Allowed Roles: SuperAdmin, Admin, HR, FinancialAdmin]
    /// </summary>
    [HttpGet("revenue-report")]
    public async Task<IActionResult> GetRevenueReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var result = await _adminService.GetRevenueReportAsync(startDate, endDate);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves all admin users, optionally filtered by roles.
    /// [Allowed Roles: SuperAdmin, HR]
    /// </summary>
    [HttpGet("users")]
    [Authorize(Roles = "SuperAdmin,HR")]
    public async Task<IActionResult> GetUsers([FromQuery] string? roles)
    {
        var roleList = roles?.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return Ok(await _authService.GetAllAdminsAsync(roleList));
    }

    /// <summary>
    /// Retrieves an admin user profile by ID. Returns 404 if the user doesn't exist.
    /// [Allowed Roles: SuperAdmin (any user), Admin / HR / FinancialAdmin (own profile only)]
    /// </summary>
    [HttpGet("users/{userId}")]
    [Authorize(Roles = "SuperAdmin,Admin,HR,FinancialAdmin")]
    public async Task<IActionResult> GetUser(Guid userId)
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId != userId.ToString() && !User.IsInRole("SuperAdmin")) return Forbid();

        var user = await _authService.GetUserAsync(userId);
        if (user == null) return NotFound(new { message = "User not found" });
        return Ok(user);
    }

    /// <summary>
    /// Updates an admin user's profile information (name and email). 
    /// [Allowed Roles: SuperAdmin (any user), Admin / HR / FinancialAdmin (own profile only)]
    /// </summary>
    [HttpPut("users/{userId}/profile")]
    [Authorize(Roles = "SuperAdmin,Admin,HR,FinancialAdmin")]
    public async Task<IActionResult> UpdateProfile(Guid userId, [FromBody] AdminUpdateProfileDto dto)
    {
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId != userId.ToString() && !User.IsInRole("SuperAdmin")) return Forbid();

        return Ok(await _authService.UpdateProfileAsync(userId, dto));
    }

    /// <summary>
    /// Activates or deactivates an admin user account.
    /// [Allowed Roles: SuperAdmin, HR]
    /// </summary>
    [HttpPut("users/{userId}/status")]
    [Authorize(Roles = "SuperAdmin,HR")]
    public async Task<IActionResult> UpdateUserStatus(Guid userId, [FromBody] AdminUpdateStatusDto dto)
    {
        await _authService.UpdateUserStatusAsync(userId, dto.IsActive);
        return Ok(new { message = "Status updated" });
    }

    /// <summary>
    /// Permanently deletes an admin user profile.
    /// [Allowed Roles: SuperAdmin Only]
    /// </summary>
    [HttpDelete("users/{userId}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {
        await _authService.DeleteUserAsync(userId);
        return Ok(new { message = "User deleted" });
    }
}
