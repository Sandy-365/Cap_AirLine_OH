using Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace AdminService.Models;

public class AdminProfile : BaseEntity<Guid>
{
    // --- Auth Credentials ---
    [Required, MaxLength(256)]
    public string Email { get; set; } = "";
    [Required]
    public string PasswordHash { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "Admin"; // Default role from Admin portal
    public bool IsEmailVerified { get; set; } = false;
    public string? VerificationToken { get; set; }
    public DateTime? VerificationTokenExpiry { get; set; }
    public bool IsActive { get; set; } = true;



    // Password Reset
    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpiry { get; set; }



    // --- Profile Fields ---
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Department { get; set; } = string.Empty;
}
