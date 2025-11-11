namespace TravelBookingSystem.Features.Booking.Common;

public abstract class BookingException : Exception
{
    protected BookingException(string message) : base(message) { }
    protected BookingException(string message, Exception innerException) 
        : base(message, innerException) { }
}


public class BookingLockUnavailableException(Guid flightId)
    : BookingException($"Could not acquire booking lock for flight '{flightId}'. Please retry.")
{
    public Guid FlightId { get; } = flightId;
}
public class DuplicateSeatNumberException(Guid flightId, int seatNumber, Exception innerException)
    : BookingException($"Duplicate seat number '{seatNumber}' detected for flight '{flightId}'.", innerException)
{
    public Guid FlightId { get; } = flightId;
    public int SeatNumber { get; } = seatNumber;
}