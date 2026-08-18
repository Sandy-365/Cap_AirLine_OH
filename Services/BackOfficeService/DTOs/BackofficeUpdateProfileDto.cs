namespace BackOfficeService.DTOs;

public class BackofficeUpdateProfileDto
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Department { get; set; }
    public string? RoleTitle { get; set; }
    public string? AssignedAirportCode { get; set; }
    public string? PhoneNumber { get; set; }
}
