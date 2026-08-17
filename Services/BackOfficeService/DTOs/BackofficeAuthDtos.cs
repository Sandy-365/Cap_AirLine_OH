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
    /// <summary>When true, account is auto-verified (no OTP email). Use when provisioned by an Admin/SuperAdmin.</summary>
    public bool ProvisionedByAdmin { get; set; } = false;
}

public class BackofficeLoginDto
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}


public class BackofficeForgotPasswordDto
{
    public string Email { get; set; } = "";
}

public class BackofficeResetPasswordDto
{
    public string Email { get; set; } = "";
    public string Token { get; set; } = "";
    public string NewPassword { get; set; } = "";
}

public class BackofficeAuthResponseDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "Staff";
    public string Token { get; set; } = "";
}

public class BackofficeUpdateProfileDto
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Department { get; set; }
    public string? RoleTitle { get; set; }
    public string? AssignedAirportCode { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? AadharNumber { get; set; }
    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public string? PassportNumber { get; set; }
}

public class BackofficeUpdateStatusDto
{
    public bool IsActive { get; set; }
}

public class BackofficeChangePasswordDto
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}
