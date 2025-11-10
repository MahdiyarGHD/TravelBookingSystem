namespace TravelBookingSystem.Features.Flight.Create;

public record CreateFlightRequest(decimal Price, string Origin, string Destination, string FlightNumber, int AvailableSeats, DateTimeOffset DepartureDate, DateTimeOffset ArrivalDate);