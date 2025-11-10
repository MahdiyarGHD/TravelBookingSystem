using FluentValidation;
using TravelBookingSystem.Features.Flight.Filter;

namespace TravelBookingSystem.Features.Flight.Filter;

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