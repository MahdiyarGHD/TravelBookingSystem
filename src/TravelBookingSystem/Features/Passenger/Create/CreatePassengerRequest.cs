namespace TravelBookingSystem.Features.Passenger.Create;

public record CreatePassengerRequest(string? PhoneNumber, string FullName, string PassportNumber, string Email);