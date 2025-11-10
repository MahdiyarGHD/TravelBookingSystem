namespace TravelBookingSystem.Features.Flight.UpdateAvailableSeats;

public record UpdateAvailableSeatsRequest(Guid FlightId, int AvailableSeats);