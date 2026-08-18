using System.Text.Json.Serialization;

namespace FlightOpsService.DTOs;

public class BookingPassengerRequestDto
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string Gender { get; set; } = "Male";
    public string? AadharCardNo { get; set; }
    public string? PassportNumber { get; set; }
    public string? Nationality { get; set; } = "Indian";
    public string? DietaryRequirements { get; set; } = "Standard";
    public string? MedicalNeeds { get; set; } = "None";
    public string? SeatNumber { get; set; }
}

public class CreateBookingDto
{
    public int FlightId { get; set; }
    public int? ScheduleId { get; set; }
    public string SeatClass { get; set; } = "Economy";
    public decimal BaggageWeight { get; set; }
    public int PassengerCount { get; set; } = 1;
    public List<BookingPassengerRequestDto>? Passengers { get; set; } = new();

    // Auto-populated internally from JWT token claims (hidden from Swagger request body)
    [JsonIgnore]
    public int? UserId { get; set; }

    [JsonIgnore]
    public string? UserEmail { get; set; }

    [JsonIgnore]
    public string? UserName { get; set; }
}
