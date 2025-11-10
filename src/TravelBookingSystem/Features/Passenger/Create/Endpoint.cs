using Carter;
using Microsoft.AspNetCore.Mvc;
using TravelBookingSystem.Common.Filters;
using TravelBookingSystem.Features.Passenger.Common;

namespace TravelBookingSystem.Features.Passenger.Create;

public class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app
            .MapGroup(FeatureManager.Prefix)
            .WithTags(FeatureManager.EndpointTagName)
            .MapPost("/",
                async ([FromBody] CreatePassengerRequest request, PassengerService service, CancellationToken cancellationToken) =>
                {
                    var passengerId = await service.CreateAsync(
                        email: request.Email,
                        phoneNumber: request.PhoneNumber,
                        passportNumber: request.PassportNumber,
                        fullName: request.FullName,
                        cancellationToken: cancellationToken
                        );
                    
                    return new CreatePassengerResponse(passengerId.ToString());
                }).AddEndpointFilter<EndpointValidatorFilter<CreatePassengerRequest>>();
    }
}