using Carter;
using Microsoft.AspNetCore.Mvc;
using TravelBookingSystem.Common.Filters;
using TravelBookingSystem.Features.Flight.Common;

namespace TravelBookingSystem.Features.Flight.UpdateAvailableSeats;

public class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app
            .MapGroup(FeatureManager.Prefix)
            .WithTags(FeatureManager.EndpointTagName)
            .MapPatch("/update-available-seats",
                async ([FromBody] UpdateAvailableSeatsRequest request, FlightService service, CancellationToken cancellationToken) =>
                {
                    await service.UpdateAvailableSeatsAsync(request.FlightId, request.AvailableSeats, cancellationToken);
                    return Results.Ok();
                }).AddEndpointFilter<EndpointValidatorFilter<UpdateAvailableSeatsRequest>>();
    }
}