using System.Text.RegularExpressions;
using FluentValidation;

namespace TravelBookingSystem.Features.Passenger.Create;

public partial class CreatePassengerRequestValidator : AbstractValidator<CreatePassengerRequest>
{
    public CreatePassengerRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.PassportNumber)
            .NotEmpty()
            .MaximumLength(32);

        RuleFor(x => x.PhoneNumber)
            .Must(p => string.IsNullOrWhiteSpace(p) || PhoneNumberMatchRegex().IsMatch(p))
            .WithMessage("Phone number must be 7–15 digits, optionally starting with +.");
    }

    [GeneratedRegex(@"^\+?\d{7,15}$")]
    private static partial Regex PhoneNumberMatchRegex();
}