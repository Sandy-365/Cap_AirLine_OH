namespace FlightOpsService.DTOs;

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
