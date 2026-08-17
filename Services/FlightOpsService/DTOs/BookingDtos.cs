using System.Text.Json.Serialization;

namespace FlightOpsService.DTOs;

public class CreateBookingDto
{
    public int FlightId { get; set; }
    public int? ScheduleId { get; set; }
    public string SeatClass { get; set; } = "Economy";
    public decimal BaggageWeight { get; set; }
    public int PassengerCount { get; set; } = 1;
    public decimal TotalAmount { get; set; }

    // Auto-populated internally from JWT token claims (hidden from Swagger request body)
    [JsonIgnore]
    public int? UserId { get; set; }

    [JsonIgnore]
    public string? UserEmail { get; set; }

    [JsonIgnore]
    public string? UserName { get; set; }
}

public class BookingDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int FlightId { get; set; }
    public int? ScheduleId { get; set; }
    public string SeatClass { get; set; } = "";
    public decimal BaggageWeight { get; set; }
    public string PNR { get; set; } = "";
    public string Status { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
    public int TotalPassengers { get; set; }
    public int ConfirmedPassengers { get; set; }
    public int CancelledPassengers { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public List<PassengerResponseDto> Passengers { get; set; } = new();
}

public class BookingHistoryDto
{
    public int Id { get; set; }
    public int FlightId { get; set; }
    public int? ScheduleId { get; set; }
    public string PNR { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
}
