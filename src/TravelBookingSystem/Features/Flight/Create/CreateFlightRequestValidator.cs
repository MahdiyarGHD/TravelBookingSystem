using FluentValidation;

namespace TravelBookingSystem.Features.Flight.Create;

public class CreateFlightRequestValidator : AbstractValidator<CreateFlightRequest>
{
    public CreateFlightRequestValidator()
    {
        RuleFor(x => x.Origin)
            .NotEmpty()
            .MaximumLength(30);

        RuleFor(x => x.Destination)
            .NotEmpty()
            .MaximumLength(30);

        RuleFor(x => x.FlightNumber)
            .NotEmpty();

        RuleFor(x => x.AvailableSeats)
            .GreaterThan(0);

        RuleFor(x => x.Price)
            .GreaterThan(0);
        
        // Date validations
        
        RuleFor(x => x)
            .Must(f => f.ArrivalDate > f.DepartureDate)
            .WithMessage("Arrival must be after departure.");
        
        RuleFor(x => x.ArrivalDate)
            .NotNull()
            .Must(arrivalDate => arrivalDate > DateTimeOffset.UtcNow)
            .WithMessage("Arrival date must be in the future.");

        RuleFor(x => x.DepartureDate)
            .NotNull();
    }
}