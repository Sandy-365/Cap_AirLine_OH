using AdminService.DTOs;
using AdminService.Models;

namespace AdminService.Interfaces;

public interface IAdminAuthService
{
    Task RegisterAsync(AdminRegisterDto dto);
    Task<AdminAuthResponseDto> VerifyAsync(AdminVerifyDto dto);
    Task ResendVerificationAsync(string email);
    Task<AdminAuthResponseDto> LoginAsync(AdminLoginDto dto);
    Task ForgotPasswordAsync(string email);
    Task ResetPasswordAsync(AdminResetPasswordDto dto);
    Task<AdminProfile?> GetUserAsync(Guid id);
    Task<AdminProfile> UpdateProfileAsync(Guid id, AdminUpdateProfileDto dto);
    Task<List<AdminProfile>> GetAllAdminsAsync(string[]? roles = null);
    Task UpdateUserStatusAsync(Guid id, bool isActive);
    Task DeleteUserAsync(Guid id);
    Task ChangePasswordAsync(Guid id, AdminChangePasswordDto dto);
}
