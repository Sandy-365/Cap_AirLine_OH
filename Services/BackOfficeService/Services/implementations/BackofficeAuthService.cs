using BackOfficeService.DTOs;
using BackOfficeService.Models;
using BackOfficeService.Repositories.Interfaces;
using BackOfficeService.Services.Interfaces;
using Shared.Security;

namespace BackOfficeService.Services.Implementations;

public class BackofficeAuthService : IBackofficeAuthService
{
    private readonly IBackofficeProfileRepository _repo;
    private readonly ITokenService _tokenService;

    public BackofficeAuthService(IBackofficeProfileRepository repo, ITokenService tokenService)
    {
        _repo = repo;
        _tokenService = tokenService;
    }

    public async Task RegisterAsync(BackofficeRegisterDto dto)
    {
        var allowedRoles = new[] { "SuperAdmin", "Admin", "HR", "FinancialAdmin", "Staff", "GroundStaff", "Dealer" };
        var requestedRole = dto.Role is not null && allowedRoles.Contains(dto.Role) ? dto.Role : "Staff";

        var existing = await _repo.GetByEmailAsync(dto.Email);
        BackofficeProfile profile;

        if (existing != null)
        {
            profile = existing;
            profile.Name = dto.Name;
            profile.PasswordHash = PasswordHasher.Hash(dto.Password);
            profile.Department = dto.Department ?? profile.Department;
            profile.RoleTitle = dto.RoleTitle ?? profile.RoleTitle;
            profile.AssignedAirportCode = dto.AssignedAirportCode ?? profile.AssignedAirportCode;
            profile.Role = requestedRole;
            profile.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(profile);
        }
        else
        {
            profile = new BackofficeProfile
            {
                Email = dto.Email,
                Name = dto.Name,
                PasswordHash = PasswordHasher.Hash(dto.Password),
                Role = requestedRole,
                Department = dto.Department ?? "",
                RoleTitle = dto.RoleTitle ?? "",
                AssignedAirportCode = dto.AssignedAirportCode ?? "",
                CreatedAt = DateTime.UtcNow
            };
            await _repo.AddAsync(profile);
        }
    }

    public async Task<BackofficeAuthResponseDto> LoginAsync(BackofficeLoginDto dto)
    {
        var profile = await _repo.GetByEmailAsync(dto.Email)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!profile.IsActive) throw new UnauthorizedAccessException("Account is deactivated.");
        if (!PasswordHasher.Verify(dto.Password, profile.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var token = _tokenService.GenerateToken(profile.Id, profile.Email, profile.Role);
        return new BackofficeAuthResponseDto
        {
            UserId = profile.Id,
            Email = profile.Email,
            Name = profile.Name,
            Role = profile.Role,
            Token = token
        };
    }

    public async Task<string> ForgotPasswordAsync(string email)
    {
        var profile = await _repo.GetByEmailAsync(email)
            ?? throw new InvalidOperationException("Account not found for the provided email.");

        profile.ResetToken = new Random().Next(100000, 999999).ToString();
        profile.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
        await _repo.UpdateAsync(profile);

        return profile.ResetToken;
    }

    public async Task ResetPasswordAsync(BackofficeResetPasswordDto dto)
    {
        var profile = await _repo.GetByEmailAsync(dto.Email)
            ?? throw new InvalidOperationException("Account not found for the provided email.");

        if (string.IsNullOrWhiteSpace(dto.Token) || profile.ResetToken != dto.Token || profile.ResetTokenExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("Invalid or expired OTP token.");

        profile.PasswordHash = PasswordHasher.Hash(dto.NewPassword);
        profile.ResetToken = null;
        profile.ResetTokenExpiry = null;
        profile.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(profile);
    }

    public async Task<BackofficeProfile> UpdateProfileAsync(int id, BackofficeUpdateProfileDto dto)
    {
        var profile = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException("User not found");
        profile.Name = dto.Name;
        profile.Department = dto.Department ?? profile.Department;
        profile.RoleTitle = dto.RoleTitle ?? profile.RoleTitle;
        profile.AssignedAirportCode = dto.AssignedAirportCode ?? profile.AssignedAirportCode;
        profile.PhoneNumber = dto.PhoneNumber;
        profile.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(profile);
        return profile;
    }

    public async Task<List<BackofficeProfile>> GetAllUsersAsync(string[]? roles = null)
    {
        return await _repo.GetAllAsync(roles);
    }

    public async Task UpdateUserStatusAsync(int id, bool isActive)
    {
        var profile = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException("User not found");
        profile.IsActive = isActive;
        profile.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(profile);
    }
}
