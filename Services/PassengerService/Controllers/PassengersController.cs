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
    private readonly IRewardService _rewardService;
    private readonly IPassengerProfileRepository _profileRepository;
    private readonly IPassengerAuthService _authService;

    public PassengersController(IRewardService rewardService, IPassengerProfileRepository profileRepository, IPassengerAuthService authService)
    {
        _rewardService = rewardService;
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

    /// <summary>
    /// Permanently deletes a passenger profile.
    /// [Allowed Roles: Admin, SuperAdmin]
    /// </summary>
    [HttpDelete("users/{userId}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        await _authService.DeleteUserAsync(userId);
        return Ok(new { message = "User deleted" });
    }

    /// <summary>
    /// Retrieves a passenger's total reward points balance.
    /// [Allowed Roles: Passenger (own balance), Admin, SuperAdmin]
    /// </summary>
    [HttpGet("rewards/{userId}/balance")]
    [Authorize]
    public async Task<ActionResult<RewardBalanceDto>> GetBalance(int userId)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId != userId.ToString() && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin")) return Forbid();

        var result = await _rewardService.GetBalanceAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// Fetches the complete reward transaction history for a passenger.
    /// [Allowed Roles: Passenger (own history), Admin, SuperAdmin]
    /// </summary>
    [HttpGet("rewards/{userId}/history")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<RewardDto>>> GetHistory(int userId)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId != userId.ToString() && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin")) return Forbid();

        var result = await _rewardService.GetHistoryAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// Awards reward points to a passenger.
    /// [Allowed Roles: Admin, SuperAdmin, Staff]
    /// </summary>
    [HttpPost("rewards/earn")]
    [Authorize(Roles = "Admin,SuperAdmin,Staff")]
    public async Task<ActionResult<RewardDto>> EarnPoints([FromBody] EarnPointsRequest request)
    {
        var result = await _rewardService.EarnPointsAsync(
            request.UserId,
            request.Points,
            request.TransactionType,
            request.BookingId);
        return Ok(result);
    }

    /// <summary>
    /// Deducts reward points from a passenger's balance for redemption.
    /// [Allowed Roles: Passenger (own points), Admin, SuperAdmin]
    /// </summary>
    [HttpPost("rewards/redeem")]
    [Authorize]
    public async Task<ActionResult<RewardDto>> RedeemPoints([FromBody] RedeemPointsRequest request)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId != request.UserId.ToString() && !User.IsInRole("Admin") && !User.IsInRole("SuperAdmin")) return Forbid();

        var result = await _rewardService.RedeemPointsAsync(request.UserId, request.Points);
        return Ok(result);
    }
}
