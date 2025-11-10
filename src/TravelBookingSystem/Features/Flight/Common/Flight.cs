namespace TravelBookingSystem.Features.Flight.Common;

public class Flight
{
    public Guid Id { get; set; }
    public string FlightNumber { get; set; }
    public string Origin { get; set; }
    public string Destination { get; set; }
    
    public DateTimeOffset DepartureTime { get; set; }
    public DateTimeOffset ArrivalTime { get; set; }
    
    public int AvailableSeats { get; set; }
    public decimal Price { get; set; }
    
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
}
