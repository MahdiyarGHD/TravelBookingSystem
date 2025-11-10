namespace TravelBookingSystem.Features.Flight.Filter;

public record FilterFlightsRequest(string? Origin, string? Destination, DateTimeOffset? DepartureDate, DateTimeOffset? ArrivalDate);