using BackOfficeService.DTOs;
using BackOfficeService.Models;
using BackOfficeService.Repositories.Interfaces;
using BackOfficeService.Services.Interfaces;
using Shared.Security;

namespace BackOfficeService.Services.Implementations;

/// <summary>
/// Service implementation for managing back-office user authentication, registration, password lifecycle, and profile management.
/// </summary>
public class BackofficeAuthService : IBackofficeAuthService
{
    private readonly IBackofficeProfileRepository _repo;
    private readonly ITokenService _tokenService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackofficeAuthService"/> class.
    /// </summary>
    /// <param name="repo">The repository for accessing and managing back-office user profile data.</param>
    /// <param name="tokenService">The service used to generate JWT authentication tokens.</param>
    public BackofficeAuthService(IBackofficeProfileRepository repo, ITokenService tokenService)
    {
        _repo = repo;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Registers a new back-office user profile or updates an existing profile with new details and role.
    /// </summary>
    /// <param name="dto">The registration data transfer object containing user information, role, and credentials.</param>
    /// <returns>A task representing the asynchronous registration operation.</returns>
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

    /// <summary>
    /// Authenticates a back-office user using their email and password credentials, returning a JWT token upon success.
    /// </summary>
    /// <param name="dto">The login credentials transfer object containing email and password.</param>
    /// <returns>A task representing the asynchronous operation that yields an authentication response containing user info and JWT token.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if the account does not exist, is deactivated, or password verification fails.</exception>
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

    /// <summary>
    /// Generates a 6-digit one-time password (OTP) reset token with a 15-minute expiration for password reset requests.
    /// </summary>
    /// <param name="email">The email address of the back-office user requesting a password reset.</param>
    /// <returns>A task representing the asynchronous operation that yields the generated reset token.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no user account is associated with the provided email.</exception>
    public async Task<string> ForgotPasswordAsync(string email)
    {
        var profile = await _repo.GetByEmailAsync(email)
            ?? throw new InvalidOperationException("Account not found for the provided email.");

        profile.ResetToken = new Random().Next(100000, 999999).ToString();
        profile.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
        await _repo.UpdateAsync(profile);

        return profile.ResetToken;
    }

    /// <summary>
    /// Resets the user's password using the verified OTP reset token.
    /// </summary>
    /// <param name="dto">The password reset data transfer object containing email, reset token, and new password.</param>
    /// <returns>A task representing the asynchronous password reset operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the account is not found, the OTP token is invalid, or the token has expired.</exception>
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

    /// <summary>
    /// Updates the profile information of a back-office user.
    /// </summary>
    /// <param name="id">The unique identifier of the user to update.</param>
    /// <param name="dto">The profile update data transfer object containing modified user details.</param>
    /// <returns>A task representing the asynchronous operation that yields the updated <see cref="BackofficeProfile"/>.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if no user profile is found matching the provided identifier.</exception>
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

    /// <summary>
    /// Retrieves all back-office user profiles, optionally filtered by specified roles.
    /// </summary>
    /// <param name="roles">An optional array of role names to filter by. If null or empty, all users are returned.</param>
    /// <returns>A task representing the asynchronous operation that yields a list of <see cref="BackofficeProfile"/> records.</returns>
    public async Task<List<BackofficeProfile>> GetAllUsersAsync(string[]? roles = null)
    {
        return await _repo.GetAllAsync(roles);
    }

    /// <summary>
    /// Updates the active/inactive status of a back-office user account.
    /// </summary>
    /// <param name="id">The unique identifier of the user whose status is to be updated.</param>
    /// <param name="isActive">The new active status value (true for active, false for deactivated).</param>
    /// <returns>A task representing the asynchronous update operation.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if no user profile is found matching the provided identifier.</exception>
    public async Task UpdateUserStatusAsync(int id, bool isActive)
    {
        var profile = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException("User not found");
        profile.IsActive = isActive;
        profile.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(profile);
    }
}
