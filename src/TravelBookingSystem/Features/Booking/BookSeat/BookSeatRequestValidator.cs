using FluentValidation;
using TravelBookingSystem.Features.Flight.Filter;

namespace TravelBookingSystem.Features.Booking.BookSeat;

public class BookSeatRequestValidator : AbstractValidator<BookSeatRequest>
{
    public BookSeatRequestValidator()
    {
        RuleFor(x => x.FlightId)
            .Must(x => Guid.TryParse(x, out _))
            .WithMessage("Invalid flight id provided.");
        
        RuleFor(x => x.PassengerId)
            .Must(x => Guid.TryParse(x, out _))
            .WithMessage("Invalid passenger id provided.");
    }
}