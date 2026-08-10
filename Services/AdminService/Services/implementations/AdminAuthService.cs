using System.Net;
using System.Net.Mail;
using AdminService.DTOs;
using AdminService.Models;
using AdminService.Data;
using AdminService.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Security;

namespace AdminService.Services;

public class AdminAuthService : IAdminAuthService
{
    private readonly AdminDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _config;

    public AdminAuthService(AdminDbContext db, ITokenService tokenService, IConfiguration config)
    {
        _db = db;
        _tokenService = tokenService;
        _config = config;
    }




    /// <summary>
    ///  Core registration logic that creates or updates an admin profile in the database.
    ///  Validates allowed roles,generates OTP tokens, and sends appropriate emails
    ///  (welcome for admin-provisioned, OTP for self-registration).
    /// </summary>
    /// 
    /// <param name="dto">  AdminRegisterDto dto - registration details </param>
    /// 
    /// <returns>   Task  </returns>
    /// 
    /// <exception cref="InvalidOperationException"></exception>
    /// 
    public async Task RegisterAsync(AdminRegisterDto dto)
    {
        var existing = await _db.AdminProfiles.FirstOrDefaultAsync(a => a.Email.ToLower() == dto.Email.ToLower());
        AdminProfile profile;

        if (existing != null)
        {
            if (existing.IsEmailVerified)
                throw new InvalidOperationException("Email already registered and verified.");

            profile = existing;
            profile.Name = dto.Name;
            profile.PasswordHash = PasswordHasher.Hash(dto.Password);
            profile.Department = dto.Department ?? profile.Department;
            profile.Role = dto.Role ?? "Admin";

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
            _db.AdminProfiles.Update(profile);
        }
        else
        {
            var allowedRoles = new[] { "Admin", "SuperAdmin", "HR", "FinancialAdmin" };
            var requestedRole = dto.Role ?? "Admin";
            if (!allowedRoles.Contains(requestedRole)) requestedRole = "Admin";

            profile = new AdminProfile
            {
                Email = dto.Email,
                Name = dto.Name,
                PasswordHash = PasswordHasher.Hash(dto.Password),
                Role = requestedRole,
                FirstName = dto.Name.Split(' ', 2).FirstOrDefault() ?? "",
                LastName = dto.Name.Split(' ', 2).Length > 1 ? dto.Name.Split(' ', 2)[1] : "",
                Department = dto.Department ?? "",
                IsEmailVerified = dto.ProvisionedByAdmin,
                VerificationToken = dto.ProvisionedByAdmin ? null : new Random().Next(100000, 999999).ToString(),
                VerificationTokenExpiry = dto.ProvisionedByAdmin ? null : DateTime.UtcNow.AddMinutes(15),
                CreatedAt = DateTime.UtcNow
            };
            await _db.AdminProfiles.AddAsync(profile);
        }
        await _db.SaveChangesAsync();

        if (dto.ProvisionedByAdmin)
        {
            try { 
                await SendWelcomeEmailAsync(dto.Email, dto.Name, dto.Password); 
            }
            catch (Exception ex) { 
                Console.WriteLine($"[WARN] Welcome email send failed: {ex.Message}");
            }
        }
        else
        {
            try { await SendOtpEmailAsync(dto.Email, dto.Name, profile.VerificationToken!); }
            catch (Exception ex) { Console.WriteLine($"[WARN] Email send failed: {ex.Message}"); }
        }
    }




    /// <summary>
    /// Validates the OTP token against the stored verification token and expiry. 
    /// Marks the account as verified, clears the token, and generates a JWT authentication token.
    /// </summary>
    /// 
    /// <param name="dto">  AdminVerifyDto dto - email and OTP token    </param>
    /// 
    /// <returns>Task of AdminAuthResponseDto</returns>
    /// 
    /// <exception cref="InvalidOperationException"></exception>
    /// 
    public async Task<AdminAuthResponseDto> VerifyAsync(AdminVerifyDto dto)
    {
        var profile = await _db.AdminProfiles.FirstOrDefaultAsync(a => a.Email.ToLower() == dto.Email.ToLower())
            ?? throw new InvalidOperationException("Account not found.");

        if (profile.IsEmailVerified) throw new InvalidOperationException("Already verified.");
        if (profile.VerificationToken != dto.Token || profile.VerificationTokenExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("Invalid or expired OTP.");

        profile.IsEmailVerified = true;
        profile.VerificationToken = null;
        profile.VerificationTokenExpiry = null;
        await _db.SaveChangesAsync();

        var token = _tokenService.GenerateToken(profile.Id, profile.Email, profile.Role);
        return new AdminAuthResponseDto { UserId = profile.Id, Email = profile.Email, Name = profile.Name, Role = profile.Role, Token = token };
    }




    /// <summary>
    /// Generates a new OTP token for unverified accounts and sends it via email. 
    /// No-ops if the account is already verified or doesn't exist.
    /// </summary>
    /// 
    /// <param name="email"> string email </param>
    /// <returns>   Task    </returns>
    public async Task ResendVerificationAsync(string email)
    {
        var profile = await _db.AdminProfiles.FirstOrDefaultAsync(a => a.Email.ToLower() == email.ToLower());
        if (profile == null || profile.IsEmailVerified) return;

        profile.VerificationToken = new Random().Next(100000, 999999).ToString();
        profile.VerificationTokenExpiry = DateTime.UtcNow.AddMinutes(15);
        await _db.SaveChangesAsync();

        try { await SendOtpEmailAsync(email, profile.Name, profile.VerificationToken!); }
        catch (Exception ex) { Console.WriteLine($"[WARN] Email send failed: {ex.Message}"); }
    }




    /// <summary>
    /// Authenticates admin credentials by verifying email, 
    /// password hash, email verification status, and account active status. 
    /// Returns user profile data and JWT token on success.
    /// </summary>
    /// 
    /// <param name="dto"></param>
    /// 
    /// <returns></returns>
    /// 
    /// <exception cref="UnauthorizedAccessException"></exception>
    /// 
    public async Task<AdminAuthResponseDto> LoginAsync(AdminLoginDto dto)
    {
        var profile = await _db.AdminProfiles.FirstOrDefaultAsync(a => a.Email.ToLower() == dto.Email.ToLower())
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!profile.IsEmailVerified) throw new UnauthorizedAccessException("Please verify your email.");
        if (!profile.IsActive) throw new UnauthorizedAccessException("Account is deactivated.");
        if (!PasswordHasher.Verify(dto.Password, profile.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var token = _tokenService.GenerateToken(profile.Id, profile.Email, profile.Role);
        return new AdminAuthResponseDto { UserId = profile.Id, Email = profile.Email, Name = profile.Name, Role = profile.Role, Token = token };
    }



    /// <summary>
    /// Generates a password reset OTP token with 15-minute expiry
    /// and emails it to the user. 
    /// Silently succeeds if the email doesn't exist for security.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public async Task ForgotPasswordAsync(string email)
    {
        var profile = await _db.AdminProfiles.FirstOrDefaultAsync(a => a.Email.ToLower() == email.ToLower());
        if (profile == null) return;

        profile.ResetToken = new Random().Next(100000, 999999).ToString();
        profile.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);
        await _db.SaveChangesAsync();

        try { await SendResetEmailAsync(email, profile.Name, profile.ResetToken!); }
        catch (Exception ex) { Console.WriteLine($"[WARN] Email send failed: {ex.Message}"); }
    }




    /// <summary>
    /// Validates the reset OTP token and updates the user's password hash.
    /// Clears the reset token after successful password change.
    /// </summary>
    /// <param name="dto">     AdminResetPasswordDto dto - email, OTP token, new password </param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task ResetPasswordAsync(AdminResetPasswordDto dto)
    {
        var profile = await _db.AdminProfiles.FirstOrDefaultAsync(a => a.Email.ToLower() == dto.Email.ToLower())
            ?? throw new InvalidOperationException("Account not found.");

        if (profile.ResetToken != dto.Token || profile.ResetTokenExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("Invalid or expired token.");

        profile.PasswordHash = PasswordHasher.Hash(dto.NewPassword);
        profile.ResetToken = null;
        profile.ResetTokenExpiry = null;
        await _db.SaveChangesAsync();
    }





    /// <summary>
    /// Retrieves a single admin profile by ID from the database. Returns null if not found.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<AdminProfile?> GetUserAsync(Guid id)
    {
        return await _db.AdminProfiles.FirstOrDefaultAsync(a => a.Id == id);
    }





    /// <summary>
    /// Updates an admin profile's name and email fields and timestamps the modification. 
    /// Throws KeyNotFoundException if the user doesn't exist.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="dto">   AdminUpdateProfileDto dto - updated name and email </param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task<AdminProfile> UpdateProfileAsync(Guid id, AdminUpdateProfileDto dto)
    {
        var profile = await _db.AdminProfiles.FirstOrDefaultAsync(a => a.Id == id) ?? throw new KeyNotFoundException("User not found");
        profile.Name = dto.Name;
        profile.Email = dto.Email;
        profile.UpdatedAt = DateTime.UtcNow;
        _db.AdminProfiles.Update(profile);
        await _db.SaveChangesAsync();
        return profile;
    }





    /// <summary>
    /// Retrieves all admin profiles from the database.
    /// Optionally filters results by one or more roles when 
    /// the roles parameter is provided.
    /// </summary>
    /// <param name="roles"></param>
    /// <returns></returns>
    public async Task<List<AdminProfile>> GetAllAdminsAsync(string[]? roles = null)
    {
        var query = _db.AdminProfiles.AsQueryable();
        
        if (roles != null && roles.Length > 0)
        {
            var rList = roles.ToList();
            query = query.Where(a => rList.Contains(a.Role));
        }

        return await query.ToListAsync();
    }




    /// <summary>
    /// Toggles an admin user's active status (activate/deactivate) 
    /// and updates the modification timestamp.
    /// Throws KeyNotFoundException if the user doesn't exist.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="isActive"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task UpdateUserStatusAsync(Guid id, bool isActive)
    {
        var profile = await _db.AdminProfiles.FirstOrDefaultAsync(a => a.Id == id) ?? throw new KeyNotFoundException("User not found");
        profile.IsActive = isActive;
        profile.UpdatedAt = DateTime.UtcNow;
        _db.AdminProfiles.Update(profile);
        await _db.SaveChangesAsync();
    }





    /// <summary>
    /// Permanently removes an admin profile from the database.
    /// Throws KeyNotFoundException if the user doesn't exist.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public async Task DeleteUserAsync(Guid id)
    {
        var profile = await _db.AdminProfiles.FirstOrDefaultAsync(a => a.Id == id) ?? throw new KeyNotFoundException("User not found");
        _db.AdminProfiles.Remove(profile);
        await _db.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(Guid id, AdminChangePasswordDto dto)
    {
        var profile = await _db.AdminProfiles.FirstOrDefaultAsync(a => a.Id == id) 
            ?? throw new InvalidOperationException("Account not found.");

        if (!PasswordHasher.Verify(dto.CurrentPassword, profile.PasswordHash))
        {
            throw new InvalidOperationException("Incorrect current password.");
        }

        profile.PasswordHash = PasswordHasher.Hash(dto.NewPassword);
        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }





    /// <summary>
    /// Sends a welcome email to admin-provisioned users containing
    /// their temporary credentials.
    /// Configured via EmailSettings in app configuration.
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
            Subject = "SkyPass Admin - Account Created",
            Body = $"Hi {name},\n\nYour admin account has been provisioned.\n\nEmail: {toEmail}\nTemporary Password: {password}\n\nPlease log in at the Admin Portal and change your password immediately.\n\nSkyPass Team"
        });
    }






    /// <summary>
    /// Sends an OTP verification email to a newly 
    /// registered admin user with the 6-digit verification code.
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
        var msg = new MailMessage(from, toEmail)
        {
            Subject = "SkyPass Admin - Verify Your Account",
            Body = $"Hi {name},\n\nYour verification OTP is: {otp}\n\nExpires in 15 minutes.\n\nSkyPass Team"
        };
        await client.SendMailAsync(msg);
    }





    /// <summary>
    /// Sends a password reset OTP email to an admin user requesting password recovery.
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
        var msg = new MailMessage(from, toEmail)
        {
            Subject = "SkyPass Admin - Password Reset OTP",
            Body = $"Hi {name},\n\nYour password reset OTP is: {otp}\n\nExpires in 15 minutes.\n\nSkyPass Team"
        };
        await client.SendMailAsync(msg);
    }
}
