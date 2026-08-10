using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using PassengerService.DTOs;
using PassengerService.Models;
using PassengerService.Repositories.Interfaces;
using PassengerService.Services.Interfaces;
using Shared.Security;

namespace PassengerService.Services.implementations;

public class PassengerAuthService : IPassengerAuthService
{
    private readonly IPassengerProfileRepository _repo;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _config;

    public PassengerAuthService(
        IPassengerProfileRepository repo,
        ITokenService tokenService,
        IConfiguration config)
    {
        _repo = repo;
        _tokenService = tokenService;
        _config = config;
    }





    /// <summary>
    /// Creates or updates a passenger profile with hashed password, OTP token, and profile fields. 
    /// Sends verification OTP email. Handles re-registration of unverified accounts.
    /// </summary>
    /// 
    /// <param name="dto"> PassengerRegisterDto dto </param>
    /// 
    /// <returns></returns>
    /// 
    /// <exception cref="InvalidOperationException"></exception>
    /// 
    public async Task RegisterAsync(PassengerRegisterDto dto)
    {
        var existing = await _repo.GetByEmailAsync(dto.Email);
        PassengerProfile profile;

        if (existing != null)
        {
            if (existing.IsEmailVerified)
                throw new InvalidOperationException("Email already registered and verified.");

            // Refresh unverified registration
            profile = existing;
            profile.Name = dto.Name;
            profile.PasswordHash = PasswordHasher.Hash(dto.Password);
            profile.FirstName = dto.Name.Split(' ').FirstOrDefault() ?? "";
            profile.LastName = dto.Name.Split(' ').Length > 1 ? string.Join(" ", dto.Name.Split(' ').Skip(1)) : "";
            profile.Phone = dto.Phone ?? profile.Phone;
            profile.DateOfBirth = ParseNullableDate(dto.DateOfBirth) ?? profile.DateOfBirth;
            profile.Aadhar = dto.Aadhar ?? profile.Aadhar;
            profile.VerificationToken = new Random().Next(100000, 999999).ToString();
            profile.VerificationTokenExpiry = DateTime.UtcNow.AddMinutes(15);
            await _repo.UpdateAsync(profile);
        }
        else
        {
            profile = new PassengerProfile
            {
                Email = dto.Email,
                Name = dto.Name,
                PasswordHash = PasswordHasher.Hash(dto.Password),
                Role = "Passenger",
                FirstName = dto.Name.Split(' ').FirstOrDefault() ?? "",
                LastName = dto.Name.Split(' ').Length > 1 ? string.Join(" ", dto.Name.Split(' ').Skip(1)) : "",
                Phone = dto.Phone ?? "",
                DateOfBirth = ParseNullableDate(dto.DateOfBirth),
                Aadhar = dto.Aadhar ?? "",
                IsEmailVerified = false,
                VerificationToken = new Random().Next(100000, 999999).ToString(),
                VerificationTokenExpiry = DateTime.UtcNow.AddMinutes(15),
                CreatedAt = DateTime.UtcNow
            };
            await _repo.AddAsync(profile);
        }

        // Send OTP email
        try { await SendOtpEmailAsync(dto.Email, dto.Name, profile.VerificationToken!); }
        catch (Exception ex) { Console.WriteLine($"[WARN] Email send failed: {ex.Message}"); }
    }

    private static DateTime? ParseNullableDate(string? dateValue)
    {
        if (string.IsNullOrWhiteSpace(dateValue))
            return null;

        return DateTime.TryParse(dateValue, out var date) ? date : null;
    }





    /// <summary>
    /// Validates OTP token, marks account as verified, 
    /// and generates JWT authentication token with user profile data.
    /// </summary>
    /// 
    /// <param name="dto">PassengerVerifyDto dto</param>
    /// 
    /// <returns>Task<PassengerAuthResponseDto></returns>
    /// 
    /// <exception cref="InvalidOperationException"></exception>
    /// 
    public async Task<PassengerAuthResponseDto> VerifyAsync(PassengerVerifyDto dto)
    {
        var profile = await _repo.GetByEmailAsync(dto.Email)
            ?? throw new InvalidOperationException("Account not found.");

        if (profile.IsEmailVerified)
            throw new InvalidOperationException("Account already verified.");

        if (profile.VerificationToken != dto.Token || profile.VerificationTokenExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("Invalid or expired OTP.");

        profile.IsEmailVerified = true;
        profile.VerificationToken = null;
        profile.VerificationTokenExpiry = null;
        await _repo.UpdateAsync(profile);

        var token = _tokenService.GenerateToken(profile.Id, profile.Email, profile.Role);
        return new PassengerAuthResponseDto
        {
            UserId = profile.Id,
            Email = profile.Email,
            Name = profile.Name,
            Role = profile.Role,
            Token = token
        };
    }






    /// <summary>
    /// Generates fresh OTP for unverified accounts and sends via email.
    /// No-ops for verified or non-existent accounts.
    /// </summary>
    /// 
    /// <param name="email"></param>
    /// 
    /// <returns></returns>
    /// 
    public async Task ResendVerificationAsync(string email)
    {
        var profile = await _repo.GetByEmailAsync(email);
        if (profile == null || profile.IsEmailVerified) return;

        profile.VerificationToken = new Random().Next(100000, 999999).ToString();
        profile.VerificationTokenExpiry = DateTime.UtcNow.AddMinutes(15);
        await _repo.UpdateAsync(profile);

        try { await SendOtpEmailAsync(email, profile.Name, profile.VerificationToken!); }
        catch (Exception ex) { Console.WriteLine($"[WARN] Email send failed: {ex.Message}"); }
    }






    /// <summary>
    /// Authenticates passenger by verifying credentials, email verification, and account status.
    /// Returns JWT token and profile data.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    /// <exception cref="UnauthorizedAccessException"></exception>
    public async Task<PassengerAuthResponseDto> LoginAsync(PassengerLoginDto dto)
    {
        var profile = await _repo.GetByEmailAsync(dto.Email)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!profile.IsEmailVerified)
            throw new UnauthorizedAccessException("Please verify your email before logging in.");

        if (!profile.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        if (!PasswordHasher.Verify(dto.Password, profile.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var token = _tokenService.GenerateToken(profile.Id, profile.Email, profile.Role);
        return new PassengerAuthResponseDto
        {
            UserId = profile.Id,
            Email = profile.Email,
            Name = profile.Name,
            Role = profile.Role,
            Token = token
        };
    }






    /// <summary>
    /// Generates password reset OTP and emails it. Silent for non-existent emails.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public async Task ForgotPasswordAsync(string email)
    {
        var profile = await _repo.GetByEmailAsync(email);
        if (profile == null) return; // Silent for security

        profile.ResetToken = new Random().Next(100000, 999999).ToString();
        profile.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
        await _repo.UpdateAsync(profile);

        try { await SendResetEmailAsync(email, profile.Name, profile.ResetToken!); }
        catch (Exception ex) { Console.WriteLine($"[WARN] Email send failed: {ex.Message}"); }
    }





    /// <summary>
    /// Validates reset OTP and updates password hash. Clears reset token after success.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task ResetPasswordAsync(PassengerResetPasswordDto dto)
    {
        var profile = await _repo.GetByEmailAsync(dto.Email)
            ?? throw new InvalidOperationException("Account not found.");

        if (profile.ResetToken != dto.Token || profile.ResetTokenExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("Invalid or expired token.");

        profile.PasswordHash = PasswordHasher.Hash(dto.NewPassword);
        profile.ResetToken = null;
        profile.ResetTokenExpiry = null;
        await _repo.UpdateAsync(profile);
    }






    /// <summary>
    ///Verifies current password and updates to new password hash. 
    ///Throws if current password is incorrect
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<PassengerProfileResponseDto?> GetUserAsync(int id)
    {
        var profile = await _repo.GetByIdAsync(id);
        return profile == null ? null : MapToResponseDto(profile);
    }




    /// <summary>
    /// Updates passenger profile fields (name, email, phone, DOB, Aadhar) and timestamps the change.
    /// </summary>
    /// 
    /// <param name="id"></param>
    /// <param name="dto">PassengerUpdateProfileDto dto</param>
    /// 
    /// <returns>Task<PassengerProfile></returns>
    /// 
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task<PassengerProfileResponseDto> UpdateProfileAsync(int id, PassengerUpdateProfileDto dto)
    {
        var profile = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException("User not found");
        profile.Name = dto.Name;
        profile.FirstName = dto.FirstName ?? profile.FirstName;
        profile.LastName = dto.LastName ?? profile.LastName;
        profile.Email = dto.Email ?? profile.Email;
        profile.Phone = dto.Phone ?? profile.Phone;
        profile.DateOfBirth = ParseNullableDate(dto.DateOfBirth) ?? profile.DateOfBirth;
        profile.Aadhar = dto.Aadhar ?? profile.Aadhar;
        profile.Gender = dto.Gender ?? profile.Gender;
        profile.PassportNumber = dto.PassportNumber ?? profile.PassportNumber;
        profile.Nationality = dto.Nationality ?? profile.Nationality;
        profile.ProfileImage = dto.ProfileImage ?? profile.ProfileImage;
        
        if (dto.TravelPreferences != null)
        {
            profile.DietaryRequirements = dto.TravelPreferences.MealType;
            profile.MedicalNeeds = dto.TravelPreferences.MedicalNeeds;
            profile.MedicalAlerts = dto.TravelPreferences.MedicalAlerts;
        }

        if (dto.SavedPassengers != null)
        {
            profile.SavedPassengers.Clear();
            profile.SavedPassengers.AddRange(dto.SavedPassengers.Select(s => new SavedPassenger
            {
                Name = s.Name,
                Age = s.Age,
                Gender = s.Gender,
                Aadhar = s.Aadhar,
                PassportNumber = s.PassportNumber,
                Nationality = s.Nationality,
                DietaryRequirements = s.DietaryRequirements,
                MedicalNeeds = s.MedicalNeeds,
                MedicalAlerts = s.MedicalAlerts,
                CreatedAt = DateTime.UtcNow
            }));
        }

        profile.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(profile);
        return MapToResponseDto(profile);
    }





    /// <summary>
    ///Verifies current password and updates to new password hash. Throws if current password is incorrect.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="UnauthorizedAccessException"></exception>
    public async Task ChangePasswordAsync(PassengerChangePasswordDto dto)
    {
        var profile = await _repo.GetByEmailAsync(dto.Email)
            ?? throw new InvalidOperationException("Account not found.");

        if (!PasswordHasher.Verify(dto.CurrentPassword, profile.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        profile.PasswordHash = PasswordHasher.Hash(dto.NewPassword);
        profile.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(profile);
    }




    /// <summary>
    /// Retrieves all passenger profiles from the database.
    /// </summary>
    /// 
    /// <returns>Task<List<PassengerProfile>></returns>
    /// 
    public async Task<List<PassengerProfileResponseDto>> GetAllPassengersAsync()
    {
        var profiles = await _repo.GetAllAsync();
        return profiles.Select(MapToResponseDto).ToList();
    }





    /// <summary>
    /// Toggles passenger account active status and updates timestamp.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="isActive"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task UpdateUserStatusAsync(int id, bool isActive)
    {
        var profile = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException("User not found");
        profile.IsActive = isActive;
        profile.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(profile);
    }





    /// <summary>
    /// oft-deletes a passenger by marking account as inactive.
    /// </summary>
    /// 
    /// <param name="id"></param>
    /// 
    /// <returns></returns>
    /// 
    /// <exception cref="KeyNotFoundException"></exception>
    /// 
    public async Task DeleteUserAsync(int id)
    {
        var profile = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException("User not found");
        // We'll need a Delete method in repo
        // For now, let's just mark as inactive if repo doesn't have Delete
        profile.IsActive = false;
        await _repo.UpdateAsync(profile);
    }






    /// <summary>
    /// Sends verification OTP email to passenger using configured SMTP settings.
    /// </summary>
    /// <param name="toEmail"></param>
    /// <param name="name"></param>
    /// <param name="otp"></param>
    /// <returns></returns>
    private async Task SendOtpEmailAsync(string toEmail, string name, string otp)
    {
        var email = _config["EmailSettings:SenderEmail"]!;
        var pwd = _config["EmailSettings:Password"]!;
        var smtp = _config["EmailSettings:SmtpServer"]!;
        var port = int.Parse(_config["EmailSettings:SmtpPort"]!);

        using var client = new SmtpClient(smtp, port)
        {
            Credentials = new NetworkCredential(email, pwd),
            EnableSsl = true
        };

        var msg = new MailMessage(email, toEmail)
        {
            Subject = "SkyPass - Verify Your Account",
            Body = $"Hi {name},\n\nYour OTP for verification is: {otp}\n\nThis OTP expires in 15 minutes.\n\nSkyPass Team",
            IsBodyHtml = false
        };
        await client.SendMailAsync(msg);
    }






    /// <summary>
    /// Sends password reset OTP email to passenger using configured SMTP settings.
    /// </summary>
    /// <param name="toEmail"></param>
    /// <param name="name"></param>
    /// <param name="otp"></param>
    /// <returns></returns>
    private async Task SendResetEmailAsync(string toEmail, string name, string otp)
    {
        var email = _config["EmailSettings:SenderEmail"]!;
        var pwd = _config["EmailSettings:Password"]!;
        var smtp = _config["EmailSettings:SmtpServer"]!;
        var port = int.Parse(_config["EmailSettings:SmtpPort"]!);

        using var client = new SmtpClient(smtp, port)
        {
            Credentials = new NetworkCredential(email, pwd),
            EnableSsl = true
        };

        var msg = new MailMessage(email, toEmail)
        {
            Subject = "SkyPass - Password Reset OTP",
            Body = $"Hi {name},\n\nYour password reset OTP is: {otp}\n\nThis OTP expires in 15 minutes.\n\nSkyPass Team",
            IsBodyHtml = false
        };
        await client.SendMailAsync(msg);
    }

    private static PassengerProfileResponseDto MapToResponseDto(PassengerProfile p)
    {
        return new PassengerProfileResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            FirstName = p.FirstName,
            LastName = p.LastName,
            Email = p.Email,
            Phone = p.Phone,
            DateOfBirth = p.DateOfBirth?.ToString("yyyy-MM-dd"),
            Aadhar = p.Aadhar,
            Gender = p.Gender,
            PassportNumber = p.PassportNumber,
            Nationality = p.Nationality,
            IsActive = p.IsActive,
            Role = p.Role,
            ProfileImage = p.ProfileImage,
            CreatedAt = p.CreatedAt,
            TravelPreferences = new TravelPreferencesDto
            {
                MealType = p.DietaryRequirements,
                MedicalNeeds = p.MedicalNeeds,
                MedicalAlerts = p.MedicalAlerts
            },
            SavedPassengers = p.SavedPassengers.Select(s => new SavedPassengerDto
            {
                Name = s.Name,
                Age = s.Age,
                Gender = s.Gender,
                Aadhar = s.Aadhar,
                PassportNumber = s.PassportNumber,
                Nationality = s.Nationality,
                DietaryRequirements = s.DietaryRequirements,
                MedicalNeeds = s.MedicalNeeds,
                MedicalAlerts = s.MedicalAlerts
            }).ToList()
        };
    }
}
