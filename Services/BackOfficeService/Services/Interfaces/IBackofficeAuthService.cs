using BackOfficeService.DTOs;
using BackOfficeService.Models;

namespace BackOfficeService.Services.Interfaces;

public interface IBackofficeAuthService
{
    Task RegisterAsync(BackofficeRegisterDto dto);
    Task<BackofficeAuthResponseDto> LoginAsync(BackofficeLoginDto dto);
    Task<string> ForgotPasswordAsync(string email);
    Task ResetPasswordAsync(BackofficeResetPasswordDto dto);
    Task<BackofficeProfile> UpdateProfileAsync(int id, BackofficeUpdateProfileDto dto);
    Task<List<BackofficeProfile>> GetAllUsersAsync(string[]? roles = null);
    Task UpdateUserStatusAsync(int id, bool isActive);
    Task ChangePasswordAsync(int id, BackofficeChangePasswordDto dto);
}
