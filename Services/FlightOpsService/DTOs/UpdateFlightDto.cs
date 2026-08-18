namespace FlightOpsService.DTOs;

public class UpdateFlightDto
{
    public DateTime? DepartureTime { get; set; }
    public DateTime? ArrivalTime { get; set; }
    public string? Gate { get; set; }
    public string? Aircraft { get; set; }
    public string? CrewAssignment { get; set; }
}
