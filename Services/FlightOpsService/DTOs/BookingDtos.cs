namespace FlightOpsService.DTOs;

public class CreateBookingDto
{
    public int UserId { get; set; }
    public int FlightId { get; set; }
    public int? ScheduleId { get; set; }
    public string SeatClass { get; set; } = "";
    public decimal BaggageWeight { get; set; }
    public int PassengerCount { get; set; } = 1;
    public string UserEmail { get; set; } = "";
    public string UserName { get; set; } = "";
    public decimal TotalAmount { get; set; }
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
