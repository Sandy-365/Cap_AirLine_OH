namespace Shared.Models;

public class BaseEntity<TId>
{
    public TId Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class BaseEntity : BaseEntity<int>
{
}

public enum UserRole
{
    SuperAdmin,
    Admin,
    HR,
    FinancialAdmin,
    Staff,
    Dealer,
    Passenger
}

public enum BookingStatus
{
    Pending,
    Confirmed,
    CheckedIn,
    Completed,
    Cancelled,
    PartiallyCancelled,
    PaymentFailed
}

public enum PaymentStatus
{
    Pending,
    Success,
    Failed,
    Refunded
}

public enum RefundStatus
{
    Pending,
    Initiated,
    Completed,
    Failed
}

public enum FlightStatus
{
    Scheduled,
    Boarding,
    Departed,
    InFlight,
    Landed,
    Delayed,
    Cancelled,
    Completed
}

public enum BaggageStatus
{
    Checked,
    Loaded,
    InTransit,
    Delivered,
    Lost
}

public enum SeatClass
{
    Economy,
    Business,
    First
}
