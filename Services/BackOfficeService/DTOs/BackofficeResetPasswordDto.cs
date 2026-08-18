namespace BackOfficeService.DTOs;

public class BackofficeResetPasswordDto
{
    public string Email { get; set; } = "";
    public string Token { get; set; } = "";
    public string NewPassword { get; set; } = "";
}
