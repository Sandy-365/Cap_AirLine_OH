using Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace BackOfficeService.Models;

public class BackofficeProfile : BaseEntity
{
    // --- Auth Credentials ---
    [Required, MaxLength(256)]
    public string Email { get; set; } = "";
    
    [Required]
    public string PasswordHash { get; set; } = "";
    
    [Required, MaxLength(150)]
    public string Name { get; set; } = "";
    
    [Required, MaxLength(50)]
    public string Role { get; set; } = "Staff"; // SuperAdmin, Admin, HR, FinancialAdmin, Staff, GroundStaff, Dealer

    public bool IsEmailVerified { get; set; } = false;
    public string? VerificationToken { get; set; }
    public DateTime? VerificationTokenExpiry { get; set; }
    
    public bool IsActive { get; set; } = true;
    public bool IsProfileComplete { get; set; } = false;
    public bool HasChangedPassword { get; set; } = false;

    // Password Reset
    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpiry { get; set; }

    // --- General Profile Fields ---
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Department { get; set; } = string.Empty;

    [MaxLength(100)]
    public string RoleTitle { get; set; } = string.Empty;

    [MaxLength(100)]
    public string AssignedAirportCode { get; set; } = string.Empty;

    // --- Personal Information ---
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(20)]
    public string? AadharNumber { get; set; }

    [MaxLength(20)]
    public string? Gender { get; set; }

    [MaxLength(50)]
    public string? Nationality { get; set; }

    [MaxLength(50)]
    public string? PassportNumber { get; set; }
}
