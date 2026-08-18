namespace PassengerService.DTOs;

public class PassengerUpdateProfileDto
{
    public string Name { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? DateOfBirth { get; set; }
    public string? Aadhar { get; set; }
    public string? Gender { get; set; }
    public string? PassportNumber { get; set; }
    public string? Nationality { get; set; }
    public byte[]? ProfileImage { get; set; }
    public TravelPreferencesDto? TravelPreferences { get; set; }
    public List<SavedPassengerDto>? SavedPassengers { get; set; }
}
