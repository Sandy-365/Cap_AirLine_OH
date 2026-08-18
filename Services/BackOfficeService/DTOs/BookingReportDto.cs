namespace BackOfficeService.DTOs;

public class BookingReportDto
{
    public int BookingId { get; set; }
    public int UserId { get; set; }
    public int FlightId { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
