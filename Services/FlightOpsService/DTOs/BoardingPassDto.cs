namespace FlightOpsService.DTOs;

public class BoardingPassDto
{
    public string PassengerName { get; set; } = "";
    public string FlightNumber { get; set; } = "";
    public string Gate { get; set; } = "";
    public string SeatNumber { get; set; } = "";
    public string QRCode { get; set; } = "";
    public DateTime DepartureTime { get; set; }
}
