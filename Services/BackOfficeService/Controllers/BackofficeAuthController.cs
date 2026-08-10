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
    /// Generates a password reset OTP token and emails it to the user.
    /// [Allowed Roles: Public (None required)]
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] BackofficeForgotPasswordDto dto)
    {
        try
        {
            await _authService.ForgotPasswordAsync(dto.Email);
            return Ok(new { message = "If the email is registered, a password reset token has been sent." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Resets password using a valid OTP token.
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
