using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PassengerService.DTOs;
using PassengerService.Models;
using PassengerService.Repositories.Interfaces;
using PassengerService.Services.Interfaces;
using System.Security.Claims;

namespace PassengerService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PassengersController : ControllerBase
{
    private readonly IPassengerProfileRepository _profileRepository;
    private readonly IPassengerAuthService _authService;

    public PassengersController(IPassengerProfileRepository profileRepository, IPassengerAuthService authService)
    {
        _profileRepository = profileRepository;
        _authService = authService;
    }

    /// <summary>
    /// Retrieves all passengers.
    /// [Allowed Roles: Admin, SuperAdmin, HR, Staff]
    /// </summary>
    [HttpGet("users")]
    [Authorize(Roles = "Admin,SuperAdmin,HR,Staff")]
    public async Task<IActionResult> GetUsers()
    {
        return Ok(await _authService.GetAllPassengersAsync());
    }

    /// <summary>
    /// Retrieves a passenger profile by ID.
    /// [Allowed Roles: Passenger (own profile), Admin, SuperAdmin, Staff]
    /// </summary>
    [HttpGet("users/{userId}")]
    [Authorize]
    public async Task<IActionResult> GetUser(int userId)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId != userId.ToString() && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin") && !User.IsInRole("Staff")) return Forbid();

        var user = await _authService.GetUserAsync(userId);
        if (user == null) return NotFound(new { message = "User not found" });
        return Ok(user);
    }

    /// <summary>
    /// Updates a passenger's profile information.
    /// [Allowed Roles: Passenger (own profile), Admin, SuperAdmin]
    /// </summary>
    [HttpPut("users/{userId}/profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(int userId, [FromBody] PassengerUpdateProfileDto dto)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId != userId.ToString() && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin")) return Forbid();

        return Ok(await _authService.UpdateProfileAsync(userId, dto));
    }

    /// <summary>
    /// Activates or deactivates a passenger account.
    /// [Allowed Roles: Admin, SuperAdmin, HR]
    /// </summary>
    [HttpPut("users/{userId}/status")]
    [Authorize(Roles = "Admin,SuperAdmin,HR")]
    public async Task<IActionResult> UpdateUserStatus(int userId, [FromBody] PassengerUpdateStatusDto dto)
    {
        await _authService.UpdateUserStatusAsync(userId, dto.IsActive);
        return Ok(new { message = "Status updated" });
    }
}

