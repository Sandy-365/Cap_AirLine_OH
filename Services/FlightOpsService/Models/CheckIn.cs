using Shared.Models;

namespace FlightOpsService.Models;

public class CheckIn : BaseEntity
{
    public int BookingId { get; set; }
    public int PassengerId { get; set; } // Per-passenger check-in
    public int UserId { get; set; }
    public int FlightId { get; set; }
    public string SeatNumber { get; set; } = "";
    public string Gate { get; set; } = "";
    public string BoardingPass { get; set; } = "";
    public string QRCode { get; set; } = "";
    public DateTime CheckInTime { get; set; }
    public bool IsCheckedIn { get; set; }
}
