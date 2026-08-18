using Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace BackOfficeService.Models;

public class BackofficeProfile : BaseEntity
{
    // --- Authentication ---
    [Required, MaxLength(256)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;
    
    [Required, MaxLength(50)]
    public string Role { get; set; } = "Staff"; // SuperAdmin, Admin, HR, FinancialAdmin, Staff, GroundStaff, Dealer
    
    public bool IsActive { get; set; } = true;

    // --- Password Reset ---
    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpiry { get; set; }

    // --- Role & Department ---
    [MaxLength(100)]
    public string Department { get; set; } = string.Empty;

    [MaxLength(100)]
    public string RoleTitle { get; set; } = string.Empty;

    [MaxLength(100)]
    public string AssignedAirportCode { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
}
