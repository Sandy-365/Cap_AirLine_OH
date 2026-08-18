namespace BackOfficeService.DTOs;

public class BackofficeAuthResponseDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "Staff";
    public string Token { get; set; } = "";
}
