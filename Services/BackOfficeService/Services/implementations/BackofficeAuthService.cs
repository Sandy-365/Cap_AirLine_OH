using BackOfficeService.Data;
using BackOfficeService.DTOs;
using BackOfficeService.Models;
using BackOfficeService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Security;

namespace BackOfficeService.Services.Implementations;

public class BackofficeAuthService : IBackofficeAuthService
{
    private readonly BackOfficeDbContext _db;
    private readonly ITokenService _tokenService;

    public BackofficeAuthService(BackOfficeDbContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    public async Task RegisterAsync(BackofficeRegisterDto dto)
    {
        var allowedRoles = new[] { "SuperAdmin", "Admin", "HR", "FinancialAdmin", "Staff", "GroundStaff", "Dealer" };
        var requestedRole = dto.Role is not null && allowedRoles.Contains(dto.Role) ? dto.Role : "Staff";

        var existing = await _db.BackofficeProfiles.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());
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
            profile.IsEmailVerified = true;
            profile.VerificationToken = null;
            profile.VerificationTokenExpiry = null;
            _db.BackofficeProfiles.Update(profile);
        }
        else
        {
            profile = new BackofficeProfile
            {
                Email = dto.Email,
                Name = dto.Name,
                PasswordHash = PasswordHasher.Hash(dto.Password),
                Role = requestedRole,
                FirstName = dto.Name.Split(' ').FirstOrDefault() ?? "",
                LastName = dto.Name.Split(' ').Length > 1 ? string.Join(" ", dto.Name.Split(' ').Skip(1)) : "",
                Department = dto.Department ?? "",
                RoleTitle = dto.RoleTitle ?? "",
                AssignedAirportCode = dto.AssignedAirportCode ?? "",
                IsEmailVerified = true,
                VerificationToken = null,
                VerificationTokenExpiry = null,
                CreatedAt = DateTime.UtcNow
            };
            await _db.BackofficeProfiles.AddAsync(profile);
        }
        await _db.SaveChangesAsync();
    }

    public async Task<BackofficeAuthResponseDto> VerifyAsync(BackofficeVerifyDto dto)
    {
        var profile = await _db.BackofficeProfiles.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower())
            ?? throw new InvalidOperationException("Account not found.");

        profile.IsEmailVerified = true;
        profile.VerificationToken = null;
        profile.VerificationTokenExpiry = null;
        await _db.SaveChangesAsync();

        var token = _tokenService.GenerateToken(profile.Id, profile.Email, profile.Role);
        return new BackofficeAuthResponseDto { UserId = profile.Id, Email = profile.Email, Name = profile.Name, Role = profile.Role, Token = token };
    }

    public async Task ResendVerificationAsync(string email)
    {
        var profile = await _db.BackofficeProfiles.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        if (profile == null) return;

        profile.IsEmailVerified = true;
        profile.VerificationToken = null;
        profile.VerificationTokenExpiry = null;
        await _db.SaveChangesAsync();
    }

    public async Task<BackofficeAuthResponseDto> LoginAsync(BackofficeLoginDto dto)
    {
        var profile = await _db.BackofficeProfiles.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower())
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!profile.IsActive) throw new UnauthorizedAccessException("Account is deactivated.");
        if (!PasswordHasher.Verify(dto.Password, profile.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var token = _tokenService.GenerateToken(profile.Id, profile.Email, profile.Role);
        return new BackofficeAuthResponseDto { UserId = profile.Id, Email = profile.Email, Name = profile.Name, Role = profile.Role, Token = token };
    }

    public async Task<string> ForgotPasswordAsync(string email)
    {
        var profile = await _db.BackofficeProfiles.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower())
            ?? throw new InvalidOperationException("Account not found for the provided email.");

        profile.ResetToken = new Random().Next(100000, 999999).ToString();
        profile.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
        await _db.SaveChangesAsync();

        return profile.ResetToken;
    }

    public async Task ResetPasswordAsync(BackofficeResetPasswordDto dto)
    {
        var profile = await _db.BackofficeProfiles.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower())
            ?? throw new InvalidOperationException("Account not found for the provided email.");

        if (string.IsNullOrWhiteSpace(dto.Token) || profile.ResetToken != dto.Token || profile.ResetTokenExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("Invalid or expired OTP token.");

        profile.PasswordHash = PasswordHasher.Hash(dto.NewPassword);
        profile.ResetToken = null;
        profile.ResetTokenExpiry = null;
        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<BackofficeProfile?> GetUserAsync(int id)
    {
        return await _db.BackofficeProfiles.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<BackofficeProfile> UpdateProfileAsync(int id, BackofficeUpdateProfileDto dto)
    {
        var profile = await _db.BackofficeProfiles.FirstOrDefaultAsync(u => u.Id == id) ?? throw new KeyNotFoundException("User not found");
        profile.Name = dto.Name;
        profile.Department = dto.Department ?? profile.Department;
        profile.RoleTitle = dto.RoleTitle ?? profile.RoleTitle;
        profile.AssignedAirportCode = dto.AssignedAirportCode ?? profile.AssignedAirportCode;
        profile.PhoneNumber = dto.PhoneNumber;
        profile.DateOfBirth = dto.DateOfBirth;
        profile.AadharNumber = dto.AadharNumber;
        profile.Gender = dto.Gender;
        profile.Nationality = dto.Nationality;
        profile.PassportNumber = dto.PassportNumber;

        profile.IsProfileComplete = !string.IsNullOrEmpty(profile.PhoneNumber) &&
                                     profile.DateOfBirth.HasValue &&
                                     !string.IsNullOrEmpty(profile.AadharNumber) &&
                                     !string.IsNullOrEmpty(profile.Gender) &&
                                     !string.IsNullOrEmpty(profile.Nationality) &&
                                     !string.IsNullOrEmpty(profile.PassportNumber);

        profile.UpdatedAt = DateTime.UtcNow;
        _db.BackofficeProfiles.Update(profile);
        await _db.SaveChangesAsync();
        return profile;
    }

    public async Task<List<BackofficeProfile>> GetAllUsersAsync(string[]? roles = null)
    {
        var query = _db.BackofficeProfiles.AsQueryable();
        if (roles != null && roles.Length > 0)
        {
            var rList = roles.ToList();
            query = query.Where(u => rList.Contains(u.Role));
        }
        return await query.ToListAsync();
    }

    public async Task UpdateUserStatusAsync(int id, bool isActive)
    {
        var profile = await _db.BackofficeProfiles.FirstOrDefaultAsync(u => u.Id == id) ?? throw new KeyNotFoundException("User not found");
        profile.IsActive = isActive;
        profile.UpdatedAt = DateTime.UtcNow;
        _db.BackofficeProfiles.Update(profile);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteUserAsync(int id)
    {
        var profile = await _db.BackofficeProfiles.FirstOrDefaultAsync(u => u.Id == id) ?? throw new KeyNotFoundException("User not found");
        _db.BackofficeProfiles.Remove(profile);
        await _db.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(int id, BackofficeChangePasswordDto dto)
    {
        var profile = await _db.BackofficeProfiles.FirstOrDefaultAsync(u => u.Id == id) ?? throw new KeyNotFoundException("User not found");
        
        if (!PasswordHasher.Verify(dto.CurrentPassword, profile.PasswordHash))
            throw new InvalidOperationException("Current password is incorrect.");

        profile.PasswordHash = PasswordHasher.Hash(dto.NewPassword);
        profile.HasChangedPassword = true;
        profile.UpdatedAt = DateTime.UtcNow;
        
        _db.BackofficeProfiles.Update(profile);
        await _db.SaveChangesAsync();
    }
}
