using Shared.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PassengerService.Models;

public class PassengerProfile : BaseEntity
{
    // --- Auth Credentials (owned by this service) ---
    [Required, MaxLength(256)]
    public string Email { get; set; } = "";
    [Required]
    public string PasswordHash { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "Passenger"; // Always Passenger from this portal
    public bool IsEmailVerified { get; set; } = false;
    public string? VerificationToken { get; set; }
    public DateTime? VerificationTokenExpiry { get; set; }
    public bool IsActive { get; set; } = true;

    // Password Reset
    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpiry { get; set; }

    // --- Profile Fields ---
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string PassportNumber { get; set; } = "";
    public string Nationality { get; set; } = "";
    public DateTime? DateOfBirth { get; set; }
    public string Phone { get; set; } = "";
    public string Aadhar { get; set; } = "";
    public string Gender { get; set; } = "";
    public string DietaryRequirements { get; set; } = "Standard";
    public string MedicalNeeds { get; set; } = "None";
    public string MedicalAlerts { get; set; } = "";
    public byte[]? ProfileImage { get; set; }

    // Relationships
    public List<SavedPassenger> SavedPassengers { get; set; } = new();

    [NotMapped]
    public List<PassengerService.Models.Reward> Rewards { get; set; } = new();
}
