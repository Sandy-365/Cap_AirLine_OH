using System.Net;
using System.Net.Mail;
using StaffService.DTOs;
using StaffService.Models;
using StaffService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Security;

namespace StaffService.Services;

public interface IStaffAuthService
{
    Task RegisterAsync(StaffRegisterDto dto);
    Task<StaffAuthResponseDto> VerifyAsync(StaffVerifyDto dto);
    Task ResendVerificationAsync(string email);
    Task<StaffAuthResponseDto> LoginAsync(StaffLoginDto dto);
    Task ForgotPasswordAsync(string email);
    Task ResetPasswordAsync(StaffResetPasswordDto dto);
    Task<StaffProfile?> GetUserAsync(int id);
    Task<StaffProfile> UpdateProfileAsync(int id, StaffUpdateProfileDto dto);
    Task<List<StaffProfile>> GetAllStaffAsync();
    Task UpdateUserStatusAsync(int id, bool isActive);
    Task DeleteUserAsync(int id);
    Task ChangePasswordAsync(int id, string currentPassword, string newPassword);
}

public class StaffAuthService : IStaffAuthService
{
    private readonly StaffDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _config;

    public StaffAuthService(StaffDbContext db, ITokenService tokenService, IConfiguration config)
    {
        _db = db;
        _tokenService = tokenService;
        _config = config;
    }





    /// <summary>
    /// Creates or updates a staff profile with validation of allowed roles. 
    /// Supports admin-provisioned (auto-verified) and self-registration (OTP required) flows. 
    /// Sends appropriate notification emails.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task RegisterAsync(StaffRegisterDto dto)
    {
        var allowedRoles = new[] { "Staff", "GroundStaff", "Dealer", "HR" };
        var requestedRole = dto.Role is not null && allowedRoles.Contains(dto.Role) ? dto.Role : "Staff";

        var existing = await _db.StaffProfiles.FirstOrDefaultAsync(s => s.Email.ToLower() == dto.Email.ToLower());
        StaffProfile profile;

        if (existing != null)
        {
            if (existing.IsEmailVerified)
                throw new InvalidOperationException("Email already registered and verified.");

            profile = existing;
            profile.Name = dto.Name;
            profile.PasswordHash = PasswordHasher.Hash(dto.Password);
            profile.Department = dto.Department ?? profile.Department;
            profile.RoleTitle = dto.RoleTitle ?? profile.RoleTitle;
            profile.AssignedAirportCode = dto.AssignedAirportCode ?? profile.AssignedAirportCode;
            profile.Role = requestedRole;

            if (dto.ProvisionedByAdmin)
            {
                // Admin-provisioned: auto-verify, no OTP needed
                profile.IsEmailVerified = true;
                profile.VerificationToken = null;
                profile.VerificationTokenExpiry = null;
            }
            else
            {
                profile.VerificationToken = new Random().Next(100000, 999999).ToString();
                profile.VerificationTokenExpiry = DateTime.UtcNow.AddMinutes(15);
            }
            _db.StaffProfiles.Update(profile);
        }
        else
        {
            profile = new StaffProfile
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
                IsEmailVerified = dto.ProvisionedByAdmin, // auto-verified if admin-provisioned
                VerificationToken = dto.ProvisionedByAdmin ? null : new Random().Next(100000, 999999).ToString(),
                VerificationTokenExpiry = dto.ProvisionedByAdmin ? null : DateTime.UtcNow.AddMinutes(15),
                CreatedAt = DateTime.UtcNow
            };
            await _db.StaffProfiles.AddAsync(profile);
        }
        await _db.SaveChangesAsync();

        if (dto.ProvisionedByAdmin)
        {
            // Send a welcome notification instead of OTP
            try { await SendWelcomeEmailAsync(dto.Email, dto.Name, dto.Password); }
            catch (Exception ex) { Console.WriteLine($"[WARN] Welcome email send failed: {ex.Message}"); }
        }
        else
        {
            try { await SendOtpEmailAsync(dto.Email, dto.Name, profile.VerificationToken!); }
            catch (Exception ex) { Console.WriteLine($"[WARN] Email send failed: {ex.Message}"); }
        }
    }





    /// <summary>
    /// Validates OTP token for staff account verification. 
    /// Marks account verified and generates JWT token.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<StaffAuthResponseDto> VerifyAsync(StaffVerifyDto dto)
    {
        var profile = await _db.StaffProfiles.FirstOrDefaultAsync(s => s.Email.ToLower() == dto.Email.ToLower())
            ?? throw new InvalidOperationException("Account not found.");

        if (profile.IsEmailVerified) throw new InvalidOperationException("Already verified.");
        if (profile.VerificationToken != dto.Token || profile.VerificationTokenExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("Invalid or expired OTP.");

        profile.IsEmailVerified = true;
        profile.VerificationToken = null;
        profile.VerificationTokenExpiry = null;
        await _db.SaveChangesAsync();

        var token = _tokenService.GenerateToken(profile.Id, profile.Email, profile.Role);
        return new StaffAuthResponseDto { UserId = profile.Id, Email = profile.Email, Name = profile.Name, Role = profile.Role, Token = token };
    }







    /// <summary>
    /// Regenerates and sends new OTP for unverified staff accounts.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public async Task ResendVerificationAsync(string email)
    {
        var profile = await _db.StaffProfiles.FirstOrDefaultAsync(s => s.Email.ToLower() == email.ToLower());
        if (profile == null || profile.IsEmailVerified) return;

        profile.VerificationToken = new Random().Next(100000, 999999).ToString();
        profile.VerificationTokenExpiry = DateTime.UtcNow.AddMinutes(15);
        await _db.SaveChangesAsync();

        try { await SendOtpEmailAsync(email, profile.Name, profile.VerificationToken!); }
        catch (Exception ex) { Console.WriteLine($"[WARN] Email send failed: {ex.Message}"); }
    }





    /// <summary>
    /// Authenticates staff credentials with verification status, active status, and password checks.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    /// <exception cref="UnauthorizedAccessException"></exception>
    public async Task<StaffAuthResponseDto> LoginAsync(StaffLoginDto dto)
    {
        var profile = await _db.StaffProfiles.FirstOrDefaultAsync(s => s.Email.ToLower() == dto.Email.ToLower())
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!profile.IsEmailVerified) throw new UnauthorizedAccessException("Please verify your email.");
        if (!profile.IsActive) throw new UnauthorizedAccessException("Account is deactivated.");
        if (!PasswordHasher.Verify(dto.Password, profile.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var token = _tokenService.GenerateToken(profile.Id, profile.Email, profile.Role);
        return new StaffAuthResponseDto { UserId = profile.Id, Email = profile.Email, Name = profile.Name, Role = profile.Role, Token = token };
    }






    /// <summary>
    /// Generates and emails password reset OTP for staff accounts.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public async Task ForgotPasswordAsync(string email)
    {
        var profile = await _db.StaffProfiles.FirstOrDefaultAsync(s => s.Email.ToLower() == email.ToLower());
        if (profile == null) return;

        profile.ResetToken = new Random().Next(100000, 999999).ToString();
        profile.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
        await _db.SaveChangesAsync();

        try { await SendResetEmailAsync(email, profile.Name, profile.ResetToken!); }
        catch (Exception ex) { Console.WriteLine($"[WARN] Email send failed: {ex.Message}"); }
    }






    /// <summary>
    /// Validates reset OTP and updates staff password hash.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task ResetPasswordAsync(StaffResetPasswordDto dto)
    {
        var profile = await _db.StaffProfiles.FirstOrDefaultAsync(s => s.Email.ToLower() == dto.Email.ToLower())
            ?? throw new InvalidOperationException("Account not found.");

        if (profile.ResetToken != dto.Token || profile.ResetTokenExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("Invalid or expired token.");

        profile.PasswordHash = PasswordHasher.Hash(dto.NewPassword);
        profile.ResetToken = null;
        profile.ResetTokenExpiry = null;
        await _db.SaveChangesAsync();
    }





    /// <summary>
    /// Retrieves staff profile by ID. Returns null if not found.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<StaffProfile?> GetUserAsync(int id)
    {
        return await _db.StaffProfiles.FirstOrDefaultAsync(s => s.Id == id);
    }





    /// <summary>
    /// Updates staff profile name and email with timestamp.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="dto"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task<StaffProfile> UpdateProfileAsync(int id, StaffUpdateProfileDto dto)
    {
        var profile = await _db.StaffProfiles.FirstOrDefaultAsync(s => s.Id == id) ?? throw new KeyNotFoundException("User not found");
        profile.Name = dto.Name;
        // Email update removed as per user request
        profile.PhoneNumber = dto.PhoneNumber;
        profile.DateOfBirth = dto.DateOfBirth;
        profile.AadharNumber = dto.AadharNumber;
        profile.Gender = dto.Gender;
        profile.Nationality = dto.Nationality;
        profile.PassportNumber = dto.PassportNumber;

        // Check if all details are filled
        profile.IsProfileComplete = !string.IsNullOrEmpty(profile.PhoneNumber) &&
                                     profile.DateOfBirth.HasValue &&
                                     !string.IsNullOrEmpty(profile.AadharNumber) &&
                                     !string.IsNullOrEmpty(profile.Gender) &&
                                     !string.IsNullOrEmpty(profile.Nationality) &&
                                     !string.IsNullOrEmpty(profile.PassportNumber);

        profile.UpdatedAt = DateTime.UtcNow;
        _db.StaffProfiles.Update(profile);
        await _db.SaveChangesAsync();
        return profile;
    }





    /// <summary>
    /// Retrieves all staff profiles from the database.
    /// </summary>
    /// <returns></returns>
    public async Task<List<StaffProfile>> GetAllStaffAsync()
    {
        return await _db.StaffProfiles.ToListAsync();
    }







    /// <summary>
    /// Toggles staff account active status.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="isActive"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task UpdateUserStatusAsync(int id, bool isActive)
    {
        var profile = await _db.StaffProfiles.FirstOrDefaultAsync(s => s.Id == id) ?? throw new KeyNotFoundException("User not found");
        profile.IsActive = isActive;
        profile.UpdatedAt = DateTime.UtcNow;
        _db.StaffProfiles.Update(profile);
        await _db.SaveChangesAsync();
    }





    /// <summary>
    /// Permanently removes a staff profile from the database.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task DeleteUserAsync(int id)
    {
        var profile = await _db.StaffProfiles.FirstOrDefaultAsync(s => s.Id == id) ?? throw new KeyNotFoundException("User not found");
        _db.StaffProfiles.Remove(profile);
        await _db.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(int id, string currentPassword, string newPassword)
    {
        var profile = await _db.StaffProfiles.FirstOrDefaultAsync(s => s.Id == id) ?? throw new KeyNotFoundException("User not found");
        
        if (!PasswordHasher.Verify(currentPassword, profile.PasswordHash))
            throw new InvalidOperationException("Current password is incorrect.");

        profile.PasswordHash = PasswordHasher.Hash(newPassword);
        profile.HasChangedPassword = true;
        profile.UpdatedAt = DateTime.UtcNow;
        
        _db.StaffProfiles.Update(profile);
        await _db.SaveChangesAsync();
    }





    /// <summary>
    /// Sends welcome email to admin-provisioned staff with temporary credentials.
    /// </summary>
    /// <param name="toEmail"></param>
    /// <param name="name"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    private async Task SendWelcomeEmailAsync(string toEmail, string name, string password)
    {
        var from = _config["EmailSettings:SenderEmail"]!;
        var pwd = _config["EmailSettings:Password"]!;
        var smtp = _config["EmailSettings:SmtpServer"]!;
        var port = int.Parse(_config["EmailSettings:SmtpPort"]!);
        using var client = new SmtpClient(smtp, port) { Credentials = new NetworkCredential(from, pwd), EnableSsl = true };
        await client.SendMailAsync(new MailMessage(from, toEmail)
        {
            Subject = "SkyPass Staff - Account Created",
            Body = $"Hi {name},\n\nYour staff account has been provisioned by an administrator.\n\nEmail: {toEmail}\nTemporary Password: {password}\n\nPlease log in at the Staff Portal and change your password immediately.\n\nSkyPass Team"
        });
    }







    /// <summary>
    /// Sends verification OTP email to newly registered staff.
    /// </summary>
    /// <param name="toEmail"></param>
    /// <param name="name"></param>
    /// <param name="otp"></param>
    /// <returns></returns>
    private async Task SendOtpEmailAsync(string toEmail, string name, string otp)
    {
        var from = _config["EmailSettings:SenderEmail"]!;
        var pwd = _config["EmailSettings:Password"]!;
        var smtp = _config["EmailSettings:SmtpServer"]!;
        var port = int.Parse(_config["EmailSettings:SmtpPort"]!);
        using var client = new SmtpClient(smtp, port) { Credentials = new NetworkCredential(from, pwd), EnableSsl = true };
        await client.SendMailAsync(new MailMessage(from, toEmail)
        {
            Subject = "SkyPass Staff - Verify Your Account",
            Body = $"Hi {name},\n\nYour verification OTP is: {otp}\n\nExpires in 15 minutes.\n\nSkyPass Team"
        });
    }





    /// <summary>
    /// Sends password reset OTP email to staff requesting recovery.
    /// </summary>
    /// <param name="toEmail"></param>
    /// <param name="name"></param>
    /// <param name="otp"></param>
    /// <returns></returns>
    private async Task SendResetEmailAsync(string toEmail, string name, string otp)
    {
        var from = _config["EmailSettings:SenderEmail"]!;
        var pwd = _config["EmailSettings:Password"]!;
        var smtp = _config["EmailSettings:SmtpServer"]!;
        var port = int.Parse(_config["EmailSettings:SmtpPort"]!);
        using var client = new SmtpClient(smtp, port) { Credentials = new NetworkCredential(from, pwd), EnableSsl = true };
        await client.SendMailAsync(new MailMessage(from, toEmail)
        {
            Subject = "SkyPass Staff - Password Reset OTP",
            Body = $"Hi {name},\n\nYour password reset OTP is: {otp}\n\nExpires in 15 minutes.\n\nSkyPass Team"
        });
    }
}
