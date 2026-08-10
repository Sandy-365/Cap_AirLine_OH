namespace StaffService.DTOs;

public class UpdateStaffDto
{
    public string Name { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Department { get; set; } = "";
    public string RoleTitle { get; set; } = "";
    public string AssignedAirportCode { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
