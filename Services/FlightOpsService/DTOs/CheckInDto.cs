namespace FlightOpsService.DTOs;

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
