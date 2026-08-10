namespace StaffService.DTOs;

public class StaffRegisterDto
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? Department { get; set; }
    public string? RoleTitle { get; set; }
    public string? AssignedAirportCode { get; set; }
    /// <summary>Role to assign: Staff, Dealer, GroundStaff, etc.</summary>
    public string? Role { get; set; }
    /// <summary>When true, account is auto-verified (no OTP email). Use when provisioned by an Admin/SuperAdmin.</summary>
    public bool ProvisionedByAdmin { get; set; } = false;
}

public class StaffLoginDto
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class StaffVerifyDto
{
    public string Email { get; set; } = "";
    public string Token { get; set; } = "";
}

public class StaffForgotPasswordDto
{
    public string Email { get; set; } = "";
}

public class StaffResetPasswordDto
{
    public string Email { get; set; } = "";
    public string? Token { get; set; }
    public string NewPassword { get; set; } = "";
}

public class StaffAuthResponseDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "Staff";
    public string Token { get; set; } = "";
}

public class StaffUpdateProfileDto
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? AadharNumber { get; set; }
    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public string? PassportNumber { get; set; }
}

public class StaffUpdateStatusDto
{
    public bool IsActive { get; set; }
}
