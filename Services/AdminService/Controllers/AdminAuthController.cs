using AdminService.DTOs;
using AdminService.Services;
using AdminService.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AdminService.Controllers;

[ApiController]
[Route("api/auth")]
public class AdminAuthController : ControllerBase
{
    private readonly IAdminAuthService _authService;
    public AdminAuthController(IAdminAuthService authService) => _authService = authService;

    /// <summary>
    /// Authenticates an admin user and returns a JWT token.
    /// [Allowed Roles: Public (None required)]
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AdminLoginDto dto)
    {
        try { return Ok(await _authService.LoginAsync(dto)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
    }

    /// <summary>
    /// Generates a password reset OTP token and sends it via email.
    /// [Allowed Roles: Public (None required)]
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] AdminForgotPasswordDto dto)
    {
        try { 
            await _authService.ForgotPasswordAsync(dto.Email); 
            return Ok(new { message = "If the email is registered, a password reset token has been sent." }); 
        }
        catch (Exception ex) { 
            return BadRequest(new { message = ex.Message }); 
        }
    }

    /// <summary>
    /// Resets an admin's password using a valid OTP token. 
    /// [Allowed Roles: Public (None required)]
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] AdminResetPasswordDto dto)
    {
        try { await _authService.ResetPasswordAsync(dto); return Ok(new { message = "Password reset successfully." }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
