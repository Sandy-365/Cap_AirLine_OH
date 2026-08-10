using Shared.Models;

namespace FlightOpsService.Models;

public class Booking : BaseEntity
{
    public int UserId { get; set; }
    public string UserEmail { get; set; } = "";
    public string UserName { get; set; } = "";
    public int FlightId { get; set; }
    public int? ScheduleId { get; set; }
    public SeatClass SeatClass { get; set; }
    public decimal BaggageWeight { get; set; }
    public string PNR { get; set; } = "";
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public int TotalPassengers { get; set; }
    public int ConfirmedPassengers { get; set; }
    public int CancelledPassengers { get; set; }
    public decimal TotalAmount { get; set; }

    // Navigation property
    public ICollection<BookingPassenger> Passengers { get; set; } = new List<BookingPassenger>();
}
