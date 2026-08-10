using Microsoft.AspNetCore.Mvc;
using PassengerService.DTOs;
using PassengerService.Services.Interfaces;

namespace PassengerService.Controllers;

[ApiController]
[Route("api/auth")]
public class PassengerAuthController : ControllerBase
{
    private readonly IPassengerAuthService _authService;
    public PassengerAuthController(IPassengerAuthService authService) => _authService = authService;

    /// <summary>
    /// Authenticates passenger credentials and returns JWT token. 
    /// [Allowed Roles: Public (None required)]
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] PassengerLoginDto dto)
    {
        try
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }
    }

    /// <summary>
    /// Registers a new passenger account with email, password, and optional profile fields.
    /// [Allowed Roles: Public (None required)]
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] PassengerRegisterDto dto)
    {
        try
        {
            await _authService.RegisterAsync(dto);
            return Ok(new { message = "Registration successful. Check your email for the OTP." });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>
    /// Resets passenger password using a valid OTP token. 
    /// [Allowed Roles: Public (None required)]
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] PassengerResetPasswordDto dto)
    {
        try
        {
            await _authService.ResetPasswordAsync(dto);
            return Ok(new { message = "Password reset successfully." });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
