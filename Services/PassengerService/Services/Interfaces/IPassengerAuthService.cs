using PassengerService.DTOs;
using PassengerService.Models;

namespace PassengerService.Services.Interfaces;

public interface IPassengerAuthService
{
    Task RegisterAsync(PassengerRegisterDto dto);
    Task<PassengerAuthResponseDto> VerifyAsync(PassengerVerifyDto dto);
    Task ResendVerificationAsync(string email);
    Task<PassengerAuthResponseDto> LoginAsync(PassengerLoginDto dto);
    Task ForgotPasswordAsync(string email);
    Task ResetPasswordAsync(PassengerResetPasswordDto dto);
    Task ChangePasswordAsync(PassengerChangePasswordDto dto);
    Task<PassengerProfileResponseDto?> GetUserAsync(int id);
    Task<PassengerProfileResponseDto> UpdateProfileAsync(int id, PassengerUpdateProfileDto dto);
    Task<List<PassengerProfileResponseDto>> GetAllPassengersAsync();
    Task UpdateUserStatusAsync(int id, bool isActive);
    Task DeleteUserAsync(int id);
}
