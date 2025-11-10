namespace TravelBookingSystem.Features.Booking.Common;

public record BookingDto(Guid Id,
    Guid PassengerId,
    string PassengerName,
    Guid FlightId,
    int SeatNumber,
    DateTimeOffset BookingDate);