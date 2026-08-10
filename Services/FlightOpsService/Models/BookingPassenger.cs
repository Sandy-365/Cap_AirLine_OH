using Shared.Models;

namespace FlightOpsService.Models;

/// <summary>
/// Represents a passenger on a booking (flight manifest passenger).
/// Renamed from Passenger to BookingPassenger to distinguish from auth-layer Passenger entity.
/// </summary>
public class BookingPassenger : BaseEntity
{
    public int BookingId { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string Gender { get; set; } = "";
    public string AadharCardNo { get; set; } = "";
    public string PassportNumber { get; set; } = "";
    public string Nationality { get; set; } = "";
    public string DietaryRequirements { get; set; } = "Standard";
    public string MedicalNeeds { get; set; } = "None";
    public string MedicalAlerts { get; set; } = "";
    public BookingPassengerStatus Status { get; set; } = BookingPassengerStatus.Confirmed;
    public decimal Fare { get; set; }       // Individual passenger fare stored at booking time
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public string? SeatNumber { get; set; }

    // Navigation property
    public Booking? Booking { get; set; }
}

public enum BookingPassengerStatus
{
    Confirmed,
    CheckedIn,
    Boarded,
    Cancelled
}
