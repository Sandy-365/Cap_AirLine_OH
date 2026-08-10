namespace PassengerService.DTOs;

// ---- Auth DTOs ----
public class PassengerRegisterDto
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? Phone { get; set; }
    public string? DateOfBirth { get; set; }
    public string? Aadhar { get; set; }
}

public class PassengerLoginDto
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class PassengerVerifyDto
{
    public string Email { get; set; } = "";
    public string Token { get; set; } = "";
}

public class PassengerForgotPasswordDto
{
    public string Email { get; set; } = "";
}

public class PassengerResetPasswordDto
{
    public string Email { get; set; } = "";
    public string Token { get; set; } = "";
    public string NewPassword { get; set; } = "";
}

public class PassengerAuthResponseDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "Passenger";
    public string Token { get; set; } = "";
}

public class PassengerUpdateProfileDto
{
    public string Name { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? DateOfBirth { get; set; }
    public string? Aadhar { get; set; }
    public string? Gender { get; set; }
    public string? PassportNumber { get; set; }
    public string? Nationality { get; set; }
    public byte[]? ProfileImage { get; set; }
    public TravelPreferencesDto? TravelPreferences { get; set; }
    public List<SavedPassengerDto>? SavedPassengers { get; set; }
}

public class TravelPreferencesDto
{
    public string MealType { get; set; } = "Standard";
    public string MedicalNeeds { get; set; } = "None";
    public string MedicalAlerts { get; set; } = "";
}

public class SavedPassengerDto
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string Gender { get; set; } = "";
    public string Aadhar { get; set; } = "";
    public string PassportNumber { get; set; } = "";
    public string Nationality { get; set; } = "";
    public string DietaryRequirements { get; set; } = "Standard";
    public string MedicalNeeds { get; set; } = "None";
    public string MedicalAlerts { get; set; } = "";
}

public class PassengerChangePasswordDto
{
    public string Email { get; set; } = "";
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}

public class PassengerUpdateStatusDto
{
    public bool IsActive { get; set; }
}

public class PassengerProfileResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string? DateOfBirth { get; set; }
    public string? Aadhar { get; set; }
    public string? Gender { get; set; }
    public string? PassportNumber { get; set; }
    public string? Nationality { get; set; }
    public bool IsActive { get; set; }
    public string Role { get; set; } = "Passenger";
    public byte[]? ProfileImage { get; set; }
    public DateTime CreatedAt { get; set; }
    public TravelPreferencesDto TravelPreferences { get; set; } = new();
    public List<SavedPassengerDto> SavedPassengers { get; set; } = new();
}
