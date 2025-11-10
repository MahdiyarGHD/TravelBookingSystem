namespace TravelBookingSystem.Features.Booking.Common;

public abstract class BookingException : Exception
{
    protected BookingException(string message) : base(message) { }
    protected BookingException(string message, Exception innerException) 
        : base(message, innerException) { }
}

public class FlightNotFoundException(Guid flightId) : BookingException($"Flight with ID '{flightId}' was not found.")
{
    public Guid FlightId { get; } = flightId;
}

public class PassengerNotFoundException(Guid passengerId)
    : BookingException($"Passenger with ID '{passengerId}' was not found.")
{
    public Guid PassengerId { get; } = passengerId;
}

public class BookingLockUnavailableException(Guid flightId)
    : BookingException($"Could not acquire booking lock for flight '{flightId}'. Please retry.")
{
    public Guid FlightId { get; } = flightId;
}

public class DuplicateBookingException(Guid passengerId, Guid flightId)
    : BookingException($"Passenger '{passengerId}' has already booked flight '{flightId}'.")
{
    public Guid PassengerId { get; } = passengerId;
    public Guid FlightId { get; } = flightId;
}

public class NoSeatsAvailableException(Guid flightId)
    : BookingException($"Flight '{flightId}' doesn't have any available seats.")
{
    public Guid FlightId { get; } = flightId;
}

public class DuplicateSeatNumberException(Guid flightId, int seatNumber, Exception innerException)
    : BookingException($"Duplicate seat number '{seatNumber}' detected for flight '{flightId}'.", innerException)
{
    public Guid FlightId { get; } = flightId;
    public int SeatNumber { get; } = seatNumber;
}