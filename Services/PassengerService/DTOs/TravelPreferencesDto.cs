namespace PassengerService.DTOs;

public class TravelPreferencesDto
{
    public string MealType { get; set; } = "Standard";
    public string MedicalNeeds { get; set; } = "None";
    public string MedicalAlerts { get; set; } = "";
}
