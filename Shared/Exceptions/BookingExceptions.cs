namespace Shared.Exceptions;

// Booking Creation
public class BookingCreationException : Exception
{
    public BookingCreationException(string message) : base(message) { }
    public BookingCreationException(string message, Exception inner) : base(message, inner) { }
}

public class BookingValidationException : Exception
{
    public Dictionary<string, string[]> ValidationErrors { get; }
    
    public BookingValidationException(Dictionary<string, string[]> validationErrors) 
        : base("Booking validation failed")
    {
        ValidationErrors = validationErrors;
    }
}

// Seat Availability
public class SeatsNotAvailableException : Exception
{
    public int FlightId { get; }
    public int? ScheduleId { get; }
    public string SeatClass { get; }
    public int RequestedSeats { get; }
    public int AvailableSeats { get; }

    public SeatsNotAvailableException(int flightId, int? scheduleId, string seatClass, 
                                      int requested, int available)
        : base($"Not enough {seatClass} seats. Requested: {requested}, Available: {available}")
    {
        FlightId = flightId;
        ScheduleId = scheduleId;
        SeatClass = seatClass;
        RequestedSeats = requested;
        AvailableSeats = available;
    }
}

// PNR Related
public class PnrNotFoundException : Exception
{
    public string PNR { get; }
    public PnrNotFoundException(string pnr) : base($"Booking with PNR '{pnr}' not found")
    {
        PNR = pnr;
    }
}

public class InvalidPNRException : Exception
{
    public string PNR { get; }
    public InvalidPNRException(string pnr) : base($"Invalid PNR format: '{pnr}'") 
    {
        PNR = pnr;
    }
}

// Booking Status
public class InvalidBookingStatusException : Exception
{
    public int BookingId { get; }
    public string CurrentStatus { get; }
    public string AttemptedAction { get; }

    public InvalidBookingStatusException(int bookingId, string currentStatus, string action)
        : base($"Cannot {action} booking {bookingId} with status '{currentStatus}'")
    {
        BookingId = bookingId;
        CurrentStatus = currentStatus;
        AttemptedAction = action;
    }
}

// Passenger Related
public class PassengerLimitExceededException : Exception
{
    public int BookingId { get; }
    public int MaxAllowed { get; }
    public int CurrentCount { get; }

    public PassengerLimitExceededException(int bookingId, int maxAllowed, int currentCount)
        : base($"Booking {bookingId} has reached maximum passenger limit ({currentCount}/{maxAllowed})")
    {
        BookingId = bookingId;
        MaxAllowed = maxAllowed;
        CurrentCount = currentCount;
    }
}

public class PassengerNotFoundException : Exception
{
    public int PassengerId { get; }
    public PassengerNotFoundException(int passengerId) : base($"Passenger {passengerId} not found")
    {
        PassengerId = passengerId;
    }
}

// Cancellation
public class BookingCancellationNotAllowedException : Exception
{
    public int BookingId { get; }
    public string Reason { get; }

    public BookingCancellationNotAllowedException(int bookingId, string reason)
        : base($"Booking {bookingId} cannot be cancelled: {reason}")
    {
        BookingId = bookingId;
        Reason = reason;
    }
}

// Missing exceptions from BookingService guide
public class BookingNotFoundException : Exception
{
    public int BookingId { get; }
    
    public BookingNotFoundException(int bookingId) 
        : base($"Booking {bookingId} not found")
    {
        BookingId = bookingId;
    }
}

public class BookingPaymentConflictException : Exception
{
    public int BookingId { get; }
    public string CurrentPaymentStatus { get; }
    public string AttemptedAction { get; }

    public BookingPaymentConflictException(int bookingId, string currentStatus, string action)
        : base($"Cannot {action} booking {bookingId} with payment status '{currentStatus}'")
    {
        BookingId = bookingId;
        CurrentPaymentStatus = currentStatus;
        AttemptedAction = action;
    }
}
