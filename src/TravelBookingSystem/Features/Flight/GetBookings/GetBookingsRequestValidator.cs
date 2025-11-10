using FluentValidation;
using TravelBookingSystem.Features.Flight.Filter;

namespace TravelBookingSystem.Features.Flight.GetBookings;

public class GetBookingsRequestValidator : AbstractValidator<GetBookingsRequest>
{
    public GetBookingsRequestValidator()
    {
        RuleFor(x => x.FlightId)
            .Must(x => Guid.TryParse(x, out _))
            .WithMessage("Invalid flight id provided.");
    }
}