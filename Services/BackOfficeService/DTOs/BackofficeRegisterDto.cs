namespace BackOfficeService.DTOs;

public class BackofficeRegisterDto
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? Department { get; set; }
    public string? RoleTitle { get; set; }
    public string? AssignedAirportCode { get; set; }
    /// <summary>Role to assign: SuperAdmin, Admin, HR, FinancialAdmin, Staff, GroundStaff, Dealer, etc.</summary>
    public string? Role { get; set; } = "Staff";
}
