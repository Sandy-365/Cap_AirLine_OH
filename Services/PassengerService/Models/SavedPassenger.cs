using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models;

namespace PassengerService.Models;

public class SavedPassenger : BaseEntity
{
    public int PassengerProfileId { get; set; }
    
    [Required]
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string Gender { get; set; } = "Male";
    public string Aadhar { get; set; } = "";
    public string PassportNumber { get; set; } = "";
    public string Nationality { get; set; } = "";
    public string DietaryRequirements { get; set; } = "Standard";
    public string MedicalNeeds { get; set; } = "None";
    public string MedicalAlerts { get; set; } = "";

    [ForeignKey("PassengerProfileId")]
    public PassengerProfile? Profile { get; set; }
}
