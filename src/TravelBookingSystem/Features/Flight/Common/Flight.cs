namespace TravelBookingSystem.Features.Flight.Common;

public class Flight
{
    public Guid Id { get; private set; }
    public string FlightNumber { get; private set; }
    public string Origin { get; private set; }
    public string Destination { get; private set; }
    
    public DateTimeOffset DepartureTime { get; private set; }
    public DateTimeOffset ArrivalTime { get; private set; }
    
    public int AvailableSeats { get; private set; }
    public decimal Price { get; private set; }
    
    public ICollection<Booking.Common.Booking> Bookings { get; set; }
    
    public static Flight Create(
        string flightNumber,
        string origin,
        string destination,
        DateTimeOffset departureTime,
        DateTimeOffset arrivalTime,
        int availableSeats,
        decimal price)
    {
        return new Flight
        {
            Id = Guid.CreateVersion7(), 
            FlightNumber = flightNumber,
            Origin = origin,
            Destination = destination,
            DepartureTime = departureTime,
            ArrivalTime = arrivalTime,
            AvailableSeats = availableSeats,
            Price = price
        };
    }

    internal void UpdateAvailableSeats(int availableSeats)
    {
        AvailableSeats = availableSeats;
    }
}
