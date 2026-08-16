using BackOfficeService.DTOs;
using BackOfficeService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackOfficeService.Controllers;

/// <summary>
/// Common Backoffice Authentication Controller handling authentication for all backoffice roles.
/// </summary>
[ApiController]
[Route("api/backoffice/auth")]
public class BackofficeAuthController : ControllerBase
{
    private readonly IBackofficeAuthService _authService;

    public BackofficeAuthController(IBackofficeAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Common Login endpoint for all backoffice roles (Admin, Staff, SuperAdmin, HR, Dealers, GroundStaff).
    /// [Allowed Roles: Public (None required)]
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] BackofficeLoginDto dto)
    {
        try { return Ok(await _authService.LoginAsync(dto)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
    }

    /// <summary>
    /// Generates a password reset OTP token and returns it directly in the response so the website can display it in an alert (no email required).
    /// [Allowed Roles: Public (None required)]
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] BackofficeForgotPasswordDto dto)
    {
        try
        {
            var token = await _authService.ForgotPasswordAsync(dto.Email);
            return Ok(new { 
                message = "Password reset OTP generated. Display this token to the user in an alert.",
                token = token,
                resetToken = token,
                expiresInMinutes = 15
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Resets password using the mandatory OTP token generated from forgot-password.
    /// [Allowed Roles: Public (None required)]
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] BackofficeResetPasswordDto dto)
    {
        try
        {
            await _authService.ResetPasswordAsync(dto);
            return Ok(new { message = "Password reset successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
