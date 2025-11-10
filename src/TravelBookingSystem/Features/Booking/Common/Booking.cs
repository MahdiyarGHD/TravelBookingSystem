namespace TravelBookingSystem.Features.Booking.Common;

public class Booking
{
    public Guid Id { get; private set; }
    public Guid PassengerId { get; private set; }
    public Guid FlightId { get; private set; }
    public int SeatNumber { get; private set; }
    public DateTimeOffset BookingDate { get; private set; }
    
    public Passenger.Common.Passenger Passenger { get; private set; }
    public Flight.Common.Flight Flight { get; private set; }
    
    public static Booking Create(
        Guid passengerId,
        Guid flightId,
        int seatNumber,
        DateTimeOffset bookingDate)
    {
        return new Booking
        {
            Id = Guid.CreateVersion7(), 
            PassengerId = passengerId,
            FlightId = flightId,
            SeatNumber = seatNumber,
            BookingDate = bookingDate
        };
    }
}
