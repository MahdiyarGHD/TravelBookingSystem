using Carter;
using EasyMicroservices.ServiceContracts;
using Microsoft.AspNetCore.Mvc;
using TravelBookingSystem.Common.Filters;
using TravelBookingSystem.Common.Persistence;
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
            .MapGet("/", async (PassengerService service, CancellationToken cancellationToken) =>
                {
                    var result = await service.GetAllAsync(cancellationToken);
                    return Results.Ok((ListMessageContract<Common.Passenger>)result);
                }
            )
            .WithSummary("Get all passengers")
            .Produces<ListMessageContract<Common.Passenger>>();
    }
}