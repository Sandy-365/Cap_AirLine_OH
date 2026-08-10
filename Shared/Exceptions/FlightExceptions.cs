namespace Shared.Exceptions;

// Flight Not Found
public class FlightNotFoundException : Exception
{
    public int FlightId { get; }
    public FlightNotFoundException(int flightId) : base($"Flight {flightId} not found")
    {
        FlightId = flightId;
    }
}

public class ScheduleNotFoundException : Exception
{
    public int ScheduleId { get; }
    public ScheduleNotFoundException(int scheduleId) : base($"Schedule {scheduleId} not found")
    {
        ScheduleId = scheduleId;
    }
}

// Flight Status
public class FlightAlreadyDepartedException : Exception
{
    public int FlightId { get; }
    public DateTime DepartureTime { get; }

    public FlightAlreadyDepartedException(int flightId, DateTime departureTime)
        : base($"Flight {flightId} has already departed at {departureTime:yyyy-MM-dd HH:mm:ss}")
    {
        FlightId = flightId;
        DepartureTime = departureTime;
    }
}

public class FlightAlreadyCompletedException : Exception
{
    public int FlightId { get; }
    public FlightAlreadyCompletedException(int flightId) 
        : base($"Flight {flightId} has already been completed")
    {
        FlightId = flightId;
    }
}

public class FlightCancelledException : Exception
{
    public int FlightId { get; }
    public FlightCancelledException(int flightId) : base($"Flight {flightId} is cancelled")
    {
        FlightId = flightId;
    }
}

// Scheduling
public class ScheduleConflictException : Exception
{
    public int FlightId { get; }
    public int ScheduleId { get; }
    public string ConflictDetails { get; }

    public ScheduleConflictException(int flightId, int scheduleId, string details)
        : base($"Schedule conflict for flight {flightId}, schedule {scheduleId}: {details}")
    {
        FlightId = flightId;
        ScheduleId = scheduleId;
        ConflictDetails = details;
    }
}

public class InvalidScheduleException : Exception
{
    public int ScheduleId { get; }
    public InvalidScheduleException(int scheduleId, string reason) 
        : base($"Invalid schedule {scheduleId}: {reason}")
    {
        ScheduleId = scheduleId;
    }
}

// Capacity
public class SeatCapacityExceededException : Exception
{
    public int FlightId { get; }
    public string SeatClass { get; }
    public int Requested { get; }
    public int Available { get; }

    public SeatCapacityExceededException(int flightId, string seatClass, int requested, int available)
        : base($"Seat capacity exceeded for flight {flightId}, class {seatClass}. Requested: {requested}, Available: {available}")
    {
        FlightId = flightId;
        SeatClass = seatClass;
        Requested = requested;
        Available = available;
    }
}

// Route
public class InvalidRouteException : Exception
{
    public string DepartureAirport { get; }
    public string Destination { get; }

    public InvalidRouteException(string departureAirport, string destination)
        : base($"Invalid route: {departureAirport} to {destination}. Source and destination cannot be the same.")
    {
        DepartureAirport = departureAirport;
        Destination = destination;
    }
}
