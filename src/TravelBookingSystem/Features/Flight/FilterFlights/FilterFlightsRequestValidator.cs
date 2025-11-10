using FluentValidation;
using TravelBookingSystem.Features.Flight.FilterFlights;

namespace TravelBookingSystem.Features.Flight.FilterFlights;

public class FilterFlightsRequestValidator : AbstractValidator<FilterFlightsRequest>
{
    public FilterFlightsRequestValidator()
    {
        RuleFor(x => x.Origin)
            .MaximumLength(30);

        RuleFor(x => x.Destination)
            .MaximumLength(30);

        RuleFor(x => x)
            .Must(f => !(f.Origin != null && f.Destination != null && f.Origin == f.Destination))
            .WithMessage("Origin and destination cannot be the same.");
    }
}