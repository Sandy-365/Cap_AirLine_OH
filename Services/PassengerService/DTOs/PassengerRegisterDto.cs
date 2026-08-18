namespace PassengerService.DTOs;

public class PassengerRegisterDto
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? Phone { get; set; }
    public string? DateOfBirth { get; set; }
    public string? Aadhar { get; set; }
}
