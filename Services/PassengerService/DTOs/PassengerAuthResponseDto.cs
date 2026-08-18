namespace PassengerService.DTOs;

public class PassengerAuthResponseDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "Passenger";
    public string Token { get; set; } = "";
}
