using FluentValidation;

namespace TravelBookingSystem.Features.Flight.UpdateAvailableSeats;

public class UpdateAvailableSeatsRequestValidator : AbstractValidator<UpdateAvailableSeatsRequest>
{
    public UpdateAvailableSeatsRequestValidator()
    {
        RuleFor(x => x.FlightId)
            .Must(x => Guid.TryParse(x, out _))
            .WithMessage("Invalid flight id provided.");
        
        RuleFor(x => x.AvailableSeats)
            .GreaterThan(0);
    }
}