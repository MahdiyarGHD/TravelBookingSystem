namespace TravelBookingSystem.Features.Flight.FilterFlights;

public record FilterFlightsRequest(string? Origin, string? Destination, DateTimeOffset? DepartureDate, DateTimeOffset? ArrivalDate);