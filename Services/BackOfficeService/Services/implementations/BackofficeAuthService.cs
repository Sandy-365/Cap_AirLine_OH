using System.Net;
using System.Net.Mail;
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
    private readonly IConfiguration _config;

    public BackofficeAuthService(BackOfficeDbContext db, ITokenService tokenService, IConfiguration config)
    {
        _db = db;
        _tokenService = tokenService;
        _config = config;
    }

    public async Task RegisterAsync(BackofficeRegisterDto dto)
    {
        var allowedRoles = new[] { "SuperAdmin", "Admin", "HR", "FinancialAdmin", "Staff", "GroundStaff", "Dealer" };
        var requestedRole = dto.Role is not null && allowedRoles.Contains(dto.Role) ? dto.Role : "Staff";

        var existing = await _db.BackofficeProfiles.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());
        BackofficeProfile profile;

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
                profile.IsEmailVerified = true;
                profile.VerificationToken = null;
                profile.VerificationTokenExpiry = null;
            }
            else
            {
                profile.VerificationToken = new Random().Next(100000, 999999).ToString();
                profile.VerificationTokenExpiry = DateTime.UtcNow.AddMinutes(15);
            }
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
                IsEmailVerified = dto.ProvisionedByAdmin,
                VerificationToken = dto.ProvisionedByAdmin ? null : new Random().Next(100000, 999999).ToString(),
                VerificationTokenExpiry = dto.ProvisionedByAdmin ? null : DateTime.UtcNow.AddMinutes(15),
                CreatedAt = DateTime.UtcNow
            };
            await _db.BackofficeProfiles.AddAsync(profile);
        }
        await _db.SaveChangesAsync();

        if (dto.ProvisionedByAdmin)
        {
            try { await SendWelcomeEmailAsync(dto.Email, dto.Name, dto.Password); }
            catch (Exception ex) { Console.WriteLine($"[WARN] Welcome email send failed: {ex.Message}"); }
        }
        else
        {
            try { await SendOtpEmailAsync(dto.Email, dto.Name, profile.VerificationToken!); }
            catch (Exception ex) { Console.WriteLine($"[WARN] Email send failed: {ex.Message}"); }
        }
    }

    public async Task<BackofficeAuthResponseDto> VerifyAsync(BackofficeVerifyDto dto)
    {
        var profile = await _db.BackofficeProfiles.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower())
            ?? throw new InvalidOperationException("Account not found.");

        if (profile.IsEmailVerified) throw new InvalidOperationException("Already verified.");
        if (profile.VerificationToken != dto.Token || profile.VerificationTokenExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("Invalid or expired OTP.");

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
        if (profile == null || profile.IsEmailVerified) return;

        profile.VerificationToken = new Random().Next(100000, 999999).ToString();
        profile.VerificationTokenExpiry = DateTime.UtcNow.AddMinutes(15);
        await _db.SaveChangesAsync();

        try { await SendOtpEmailAsync(email, profile.Name, profile.VerificationToken!); }
        catch (Exception ex) { Console.WriteLine($"[WARN] Email send failed: {ex.Message}"); }
    }

    public async Task<BackofficeAuthResponseDto> LoginAsync(BackofficeLoginDto dto)
    {
        var profile = await _db.BackofficeProfiles.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower())
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!profile.IsEmailVerified) throw new UnauthorizedAccessException("Please verify your email.");
        if (!profile.IsActive) throw new UnauthorizedAccessException("Account is deactivated.");
        if (!PasswordHasher.Verify(dto.Password, profile.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var token = _tokenService.GenerateToken(profile.Id, profile.Email, profile.Role);
        return new BackofficeAuthResponseDto { UserId = profile.Id, Email = profile.Email, Name = profile.Name, Role = profile.Role, Token = token };
    }

    public async Task ForgotPasswordAsync(string email)
    {
        var profile = await _db.BackofficeProfiles.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        if (profile == null) return;

        profile.ResetToken = new Random().Next(100000, 999999).ToString();
        profile.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
        await _db.SaveChangesAsync();

        try { await SendResetEmailAsync(email, profile.Name, profile.ResetToken!); }
        catch (Exception ex) { Console.WriteLine($"[WARN] Email send failed: {ex.Message}"); }
    }

    public async Task ResetPasswordAsync(BackofficeResetPasswordDto dto)
    {
        var profile = await _db.BackofficeProfiles.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower())
            ?? throw new InvalidOperationException("Account not found.");

        if (profile.ResetToken != dto.Token || profile.ResetTokenExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("Invalid or expired token.");

        profile.PasswordHash = PasswordHasher.Hash(dto.NewPassword);
        profile.ResetToken = null;
        profile.ResetTokenExpiry = null;
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

    private async Task SendWelcomeEmailAsync(string toEmail, string name, string password)
    {
        var from = _config["EmailSettings:SenderEmail"]!;
        var pwd = _config["EmailSettings:Password"]!;
        var smtp = _config["EmailSettings:SmtpServer"]!;
        var port = int.Parse(_config["EmailSettings:SmtpPort"]!);
        using var client = new SmtpClient(smtp, port) { Credentials = new NetworkCredential(from, pwd), EnableSsl = true };
        await client.SendMailAsync(new MailMessage(from, toEmail)
        {
            Subject = "SkyPass Backoffice - Account Created",
            Body = $"Hi {name},\n\nYour account has been provisioned.\n\nEmail: {toEmail}\nTemporary Password: {password}\n\nPlease log in and change your password immediately.\n\nSkyPass Team"
        });
    }

    private async Task SendOtpEmailAsync(string toEmail, string name, string otp)
    {
        var from = _config["EmailSettings:SenderEmail"]!;
        var pwd = _config["EmailSettings:Password"]!;
        var smtp = _config["EmailSettings:SmtpServer"]!;
        var port = int.Parse(_config["EmailSettings:SmtpPort"]!);
        using var client = new SmtpClient(smtp, port) { Credentials = new NetworkCredential(from, pwd), EnableSsl = true };
        await client.SendMailAsync(new MailMessage(from, toEmail)
        {
            Subject = "SkyPass Backoffice - Verify Your Account",
            Body = $"Hi {name},\n\nYour verification OTP is: {otp}\n\nExpires in 15 minutes.\n\nSkyPass Team"
        });
    }

    private async Task SendResetEmailAsync(string toEmail, string name, string otp)
    {
        var from = _config["EmailSettings:SenderEmail"]!;
        var pwd = _config["EmailSettings:Password"]!;
        var smtp = _config["EmailSettings:SmtpServer"]!;
        var port = int.Parse(_config["EmailSettings:SmtpPort"]!);
        using var client = new SmtpClient(smtp, port) { Credentials = new NetworkCredential(from, pwd), EnableSsl = true };
        await client.SendMailAsync(new MailMessage(from, toEmail)
        {
            Subject = "SkyPass Backoffice - Password Reset OTP",
            Body = $"Hi {name},\n\nYour password reset OTP is: {otp}\n\nExpires in 15 minutes.\n\nSkyPass Team"
        });
    }
}
