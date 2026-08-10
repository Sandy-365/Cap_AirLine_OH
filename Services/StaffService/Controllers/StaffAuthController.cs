using StaffService.DTOs;
using StaffService.Services;
using Microsoft.AspNetCore.Mvc;

namespace StaffService.Controllers;

[ApiController]
[Route("api/auth")]
public class StaffAuthController : ControllerBase
{
    private readonly IStaffAuthService _authService;
    public StaffAuthController(IStaffAuthService authService) => _authService = authService;

    /// <summary>
    /// Authenticates staff credentials and returns JWT token.
    /// [Allowed Roles: Public (None required)]
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] StaffLoginDto dto)
    {
        try { return Ok(await _authService.LoginAsync(dto)); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
    }

    /// <summary>
    /// Registers a new staff account with email, password, and role. 
    /// Sends OTP for self-registration or welcome email for admin-provisioned accounts. 
    /// [Allowed Roles: Public (None required)]
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] StaffRegisterDto dto)
    {
        try { await _authService.RegisterAsync(dto); return Ok(new { message = "Registration successful. Check your email for the OTP." }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>
    /// Resets staff password using valid OTP token.
    /// [Allowed Roles: Public (None required)]
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] StaffResetPasswordDto dto)
    {
        try { await _authService.ResetPasswordAsync(dto); return Ok(new { message = "Password reset successfully." }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
