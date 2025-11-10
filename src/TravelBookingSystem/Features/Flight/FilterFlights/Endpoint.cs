using Carter;
using Microsoft.AspNetCore.Mvc;
using TravelBookingSystem.Common.Filters;
using TravelBookingSystem.Features.Flight.Common;

namespace TravelBookingSystem.Features.Flight.FilterFlights;

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
                            arrivalDate: request.ArrivalDate
                    );
                    
                    return Results.Ok(flights);
                }).AddEndpointFilter<EndpointValidatorFilter<FilterFlightsRequest>>();
    }
}