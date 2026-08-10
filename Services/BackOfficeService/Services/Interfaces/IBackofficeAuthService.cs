using BackOfficeService.DTOs;
using BackOfficeService.Models;

namespace BackOfficeService.Services.Interfaces;

public interface IBackofficeAuthService
{
    Task RegisterAsync(BackofficeRegisterDto dto);
    Task<BackofficeAuthResponseDto> VerifyAsync(BackofficeVerifyDto dto);
    Task ResendVerificationAsync(string email);
    Task<BackofficeAuthResponseDto> LoginAsync(BackofficeLoginDto dto);
    Task ForgotPasswordAsync(string email);
    Task ResetPasswordAsync(BackofficeResetPasswordDto dto);
    Task<BackofficeProfile?> GetUserAsync(int id);
    Task<BackofficeProfile> UpdateProfileAsync(int id, BackofficeUpdateProfileDto dto);
    Task<List<BackofficeProfile>> GetAllUsersAsync(string[]? roles = null);
    Task UpdateUserStatusAsync(int id, bool isActive);
    Task DeleteUserAsync(int id);
    Task ChangePasswordAsync(int id, BackofficeChangePasswordDto dto);
}
