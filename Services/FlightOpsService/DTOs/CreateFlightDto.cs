namespace FlightOpsService.DTOs;

public class CreateFlightDto
{
    public string FlightNumber { get; set; } = "";
    public string Source { get; set; } = "";
    public string Destination { get; set; } = "";
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public string Aircraft { get; set; } = "";
    public int TotalSeats { get; set; }
    public int EconomySeats { get; set; }
    public int BusinessSeats { get; set; }
    public int FirstSeats { get; set; }
    public decimal EconomyPrice { get; set; }
    public decimal BusinessPrice { get; set; }
    public decimal FirstClassPrice { get; set; }
}
