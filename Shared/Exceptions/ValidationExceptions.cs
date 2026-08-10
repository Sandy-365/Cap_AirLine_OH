namespace Shared.Exceptions;

// General Validation
public class DomainValidationException : Exception
{
    public string PropertyName { get; }
    public object? InvalidValue { get; }
    public string ValidationRule { get; }

    public DomainValidationException(string propertyName, object? invalidValue, string rule)
        : base($"Validation failed for '{propertyName}': {rule}")
    {
        PropertyName = propertyName;
        InvalidValue = invalidValue;
        ValidationRule = rule;
    }
}

// Baggage
public class BaggageWeightExceededException : Exception
{
    public decimal ActualWeight { get; }
    public decimal MaxAllowed { get; }

    public BaggageWeightExceededException(decimal actualWeight, decimal maxAllowed)
        : base($"Baggage weight {actualWeight}kg exceeds maximum allowed {maxAllowed}kg")
    {
        ActualWeight = actualWeight;
        MaxAllowed = maxAllowed;
    }
}

public class BaggageNotFoundException : Exception
{
    public string BaggageId { get; }
    public BaggageNotFoundException(string baggageId) : base($"Baggage {baggageId} not found")
    {
        BaggageId = baggageId;
    }
}

// Check-In
public class CheckInAlreadyCompletedException : Exception
{
    public int BookingId { get; }
    public int PassengerId { get; }

    public CheckInAlreadyCompletedException(int bookingId, int passengerId)
        : base($"Check-in already completed for booking {bookingId}, passenger {passengerId}")
    {
        BookingId = bookingId;
        PassengerId = passengerId;
    }
}

public class CheckInWindowClosedException : Exception
{
    public int FlightId { get; }
    public CheckInWindowClosedException(int flightId)
        : base($"Check-in window is closed for flight {flightId}")
    {
        FlightId = flightId;
    }
}

public class SeatAssignmentException : Exception
{
    public string Reason { get; }
    public SeatAssignmentException(string reason) : base($"Seat assignment failed: {reason}")
    {
        Reason = reason;
    }
}

// Rewards
public class InsufficientRewardPointsException : Exception
{
    public int UserId { get; }
    public int Required { get; }
    public int Available { get; }

    public InsufficientRewardPointsException(int userId, int required, int available)
        : base($"Insufficient reward points for user {userId}. Required: {required}, Available: {available}")
    {
        UserId = userId;
        Required = required;
        Available = available;
    }
}

// Notification
public class NotificationDeliveryException : Exception
{
    public string NotificationType { get; }
    public string? FailureReason { get; }

    public NotificationDeliveryException(string notificationType, string? reason = null)
        : base($"Failed to deliver notification: {notificationType}. Reason: {reason ?? "Unknown"}")
    {
        NotificationType = notificationType;
        FailureReason = reason;
    }
}
