namespace FlightOpsService.DTOs;

public class CreateScheduleDto
{
    public int FlightId { get; set; }
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public string Gate { get; set; } = "";
    public int EconomySeats { get; set; }
    public int BusinessSeats { get; set; }
    public int FirstSeats { get; set; }
    public decimal EconomyPrice { get; set; }
    public decimal BusinessPrice { get; set; }
    public decimal FirstClassPrice { get; set; }
}
