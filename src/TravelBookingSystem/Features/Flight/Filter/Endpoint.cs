using Carter;
using EasyMicroservices.ServiceContracts;
using Microsoft.AspNetCore.Mvc;
using TravelBookingSystem.Common.Filters;
using TravelBookingSystem.Features.Flight.Common;

namespace TravelBookingSystem.Features.Flight.Filter;

public class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app
            .MapGroup(FeatureManager.Prefix)
            .WithTags(FeatureManager.EndpointTagName)
            .MapGet("/filter",
                async ([AsParameters] FilterFlightsRequest request, FlightService service, CancellationToken cancellationToken) =>
                {
                    var flights = await service.FilterAsync(
                            origin: request.Origin,
                            destination: request.Destination,
                            departureDate: request.DepartureDate,
                            arrivalDate: request.ArrivalDate,
                            cancellationToken: cancellationToken
                    );
                    
                    return Results.Ok((ListMessageContract<Common.Flight>)flights);
                })
            .WithSummary("Filter flights by criteria")
            .AddEndpointFilter<EndpointValidatorFilter<FilterFlightsRequest>>()
            .Produces<ListMessageContract<Common.Flight>>();
    }
}