using FluentValidation;

namespace TravelBookingSystem.Features.Flight.UpdateAvailableSeats;

public class UpdateAvailableSeatsRequestValidator : AbstractValidator<UpdateAvailableSeatsRequest>
{
    public UpdateAvailableSeatsRequestValidator()
    {
        RuleFor(x => x.FlightId)
            .NotEmpty();
        
        RuleFor(x => x.AvailableSeats)
            .GreaterThan(0);
    }
}