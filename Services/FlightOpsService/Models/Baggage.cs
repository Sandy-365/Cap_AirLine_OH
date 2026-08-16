using Shared.Models;

namespace FlightOpsService.Models;

public class Baggage : BaseEntity
{
    // Foreign Key & Navigation Property
    public int BookingId { get; set; }
    public Booking? Booking { get; set; }

    public decimal Weight { get; set; }
    public string PassengerName { get; set; } = "";
    public string FlightNumber { get; set; } = "";
    public BaggageStatus Status { get; set; } = BaggageStatus.Checked;
    public bool IsDelivered { get; set; }
    public string TrackingNumber { get; set; } = "";
}
