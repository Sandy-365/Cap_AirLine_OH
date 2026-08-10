namespace PassengerService.DTOs;

public class UpsertPassengerProfileDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Phone { get; set; } = "";
    public DateTime? DateOfBirth { get; set; }
    public string Nationality { get; set; } = "";
    public string Aadhar { get; set; } = "";
    public string PassportNumber { get; set; } = "";
}
