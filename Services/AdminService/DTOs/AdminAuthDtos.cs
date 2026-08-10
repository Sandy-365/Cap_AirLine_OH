namespace AdminService.DTOs;

public class AdminRegisterDto
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? Department { get; set; }
    public string? Role { get; set; } = "Admin";
    public bool ProvisionedByAdmin { get; set; } = false;
}

public class AdminLoginDto
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class AdminVerifyDto
{
    public string Email { get; set; } = "";
    public string Token { get; set; } = "";
}

public class AdminForgotPasswordDto
{
    public string Email { get; set; } = "";
}

public class AdminResetPasswordDto
{
    public string Email { get; set; } = "";
    public string Token { get; set; } = "";
    public string NewPassword { get; set; } = "";
}

public class AdminAuthResponseDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "Admin";
    public string Token { get; set; } = "";
}

public class AdminUpdateProfileDto
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

public class AdminUpdateStatusDto
{
    public bool IsActive { get; set; }
}

public class AdminChangePasswordDto
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}
