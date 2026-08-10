using System.ComponentModel.DataAnnotations;

namespace StaffService.DTOs;

public class CreateStaffDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, MinLength(6)]
    public string Password { get; set; } = "";

    [Required]
    public string Name { get; set; } = "";

    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Department { get; set; } = "";
    public string RoleTitle { get; set; } = "";
    public string AssignedAirportCode { get; set; } = "";
}
