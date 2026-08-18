using System.Text.Json.Serialization;

namespace FlightOpsService.DTOs;

public class OnlineCheckInDto
{
    public int BookingId { get; set; }
    public int PassengerId { get; set; } // Track specifically which passenger checked in
    public string? SeatNumber { get; set; }

    // Auto-populated internally from JWT token claims (hidden from Swagger request body)
    [JsonIgnore]
    public int? UserId { get; set; }
}
