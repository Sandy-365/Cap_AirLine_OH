using BackOfficeService.DTOs;
using BackOfficeService.Models;

namespace BackOfficeService.Services.Interfaces;

/// <summary>
/// Interface for back-office authentication, registration, password lifecycle, and profile management operations.
/// </summary>
public interface IBackofficeAuthService
{
    /// <summary>
    /// Registers a new back-office user profile or updates an existing profile with new details and role.
    /// </summary>
    Task RegisterAsync(BackofficeRegisterDto dto);

    /// <summary>
    /// Authenticates a back-office user with email and password, returning user details and JWT token.
    /// </summary>
    Task<BackofficeAuthResponseDto> LoginAsync(BackofficeLoginDto dto);

    /// <summary>
    /// Generates a 6-digit OTP reset token for password reset.
    /// </summary>
    Task<string> ForgotPasswordAsync(string email);

    /// <summary>
    /// Resets the user's password using the verified OTP reset token.
    /// </summary>
    Task ResetPasswordAsync(BackofficeResetPasswordDto dto);

    /// <summary>
    /// Updates the profile information of a back-office user.
    /// </summary>
    Task<BackofficeProfile> UpdateProfileAsync(int id, BackofficeUpdateProfileDto dto);

    /// <summary>
    /// Retrieves all back-office user profiles, optionally filtered by specified roles.
    /// </summary>
    Task<List<BackofficeProfile>> GetAllUsersAsync(string[]? roles = null);

    /// <summary>
    /// Updates the active/inactive status of a back-office user account.
    /// </summary>
    Task UpdateUserStatusAsync(int id, bool isActive);
}
