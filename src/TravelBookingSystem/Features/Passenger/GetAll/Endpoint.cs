using Carter;
using Microsoft.AspNetCore.Mvc;
using TravelBookingSystem.Common.Filters;
using TravelBookingSystem.Features.Passenger.Common;
using TravelBookingSystem.Features.Passenger.Create;

namespace TravelBookingSystem.Features.Passenger.GetAll;

public class Endpoint : ICarterModule
{
    // this endpoint is not required at all
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app
            .MapGroup(FeatureManager.Prefix)
            .WithTags(FeatureManager.EndpointTagName)
            .MapGet("/",
                async (PassengerService service, CancellationToken cancellationToken) =>
                    Results.Ok(await service.GetAllAsync(cancellationToken)));
    }
}