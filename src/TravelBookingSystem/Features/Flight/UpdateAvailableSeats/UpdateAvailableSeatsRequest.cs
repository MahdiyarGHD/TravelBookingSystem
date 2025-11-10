namespace TravelBookingSystem.Features.Flight.UpdateAvailableSeats;

public record UpdateAvailableSeatsRequest(string FlightId, int AvailableSeats);