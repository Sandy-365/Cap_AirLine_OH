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

public class StaffCheckInDto
{
    public int BookingId { get; set; }
    public int PassengerId { get; set; }
    public int FlightId { get; set; }
    public string? SeatNumber { get; set; }
    public string? Gate { get; set; }
    public string PassengerName { get; set; } = "";
    public string FlightNumber { get; set; } = "";
    public decimal Fare { get; set; }
    public int UserId { get; set; }
}

public class CheckInDto
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int PassengerId { get; set; }
    public string PassengerName { get; set; } = "";
    public string FlightNumber { get; set; } = "";
    public string SeatNumber { get; set; } = "";
    public string Gate { get; set; } = "";
    public string BoardingPass { get; set; } = "";
    public DateTime CheckInTime { get; set; }
}

public class BoardingPassDto
{
    public string PassengerName { get; set; } = "";
    public string FlightNumber { get; set; } = "";
    public string Gate { get; set; } = "";
    public string SeatNumber { get; set; } = "";
    public string QRCode { get; set; } = "";
    public DateTime DepartureTime { get; set; }
}

public class CheckInSummaryDto
{
    public int TotalCheckIns { get; set; }
    public int TodayCheckIns { get; set; }
}
