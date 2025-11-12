using Carter;
using EasyMicroservices.ServiceContracts;
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
                    var flightId = Guid.Parse(request.FlightId);
                    
                    var result = await service.UpdateAvailableSeatsAsync(flightId, request.AvailableSeats, cancellationToken);

                    if (!result)
                        return Results.BadRequest(result.ToContract());
                    
                    return Results.Ok((MessageContract)true);
                })
            .WithSummary("Update available seats (flight's capacity) for a flight")
            .AddEndpointFilter<EndpointValidatorFilter<UpdateAvailableSeatsRequest>>()
            .Produces<MessageContract>();
    }
}