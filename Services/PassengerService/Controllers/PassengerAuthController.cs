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
    /// Verifies passenger email account using the OTP received via email and returns JWT token upon successful verification.
    /// [Allowed Roles: Public (None required)]
    /// </summary>
    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] PassengerVerifyDto dto)
    {
        try
        {
            var result = await _authService.VerifyAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>
    /// Resends verification OTP to the specified passenger email address.
    /// [Allowed Roles: Public (None required)]
    /// </summary>
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] PassengerForgotPasswordDto dto)
    {
        try
        {
            await _authService.ResendVerificationAsync(dto.Email);
            return Ok(new { message = "If the account exists and is unverified, a new OTP has been sent." });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>
    /// Directly verifies passenger email address and returns JWT token (bypasses OTP check for testing/convenience).
    /// [Allowed Roles: Public (None required)]
    /// </summary>
    [HttpPost("force-verify")]
    public async Task<IActionResult> ForceVerify([FromBody] PassengerForgotPasswordDto dto)
    {
        try
        {
            var result = await _authService.ForceVerifyAsync(dto.Email);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>
    /// Generates a password reset OTP token and returns it directly in the response so the website can alert it (no email required).
    /// [Allowed Roles: Public (None required)]
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] PassengerForgotPasswordDto dto)
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
    /// Resets passenger password using the mandatory OTP token generated from forgot-password.
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
