using Carter;
using Microsoft.AspNetCore.Mvc;
using TravelBookingSystem.Common.Filters;
using TravelBookingSystem.Features.Flight.Common;
using TravelBookingSystem.Features.Flight.Filter;

namespace TravelBookingSystem.Features.Flight.GetBookings;

public class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app
            .MapGroup(FeatureManager.Prefix)
            .WithTags(FeatureManager.EndpointTagName)
            .MapGet("/get-bookings",
                async ([AsParameters] GetBookingsRequest request, FlightService service, CancellationToken cancellationToken) =>
                {
                    var flightId = Guid.Parse(request.FlightId);
                    
                    var flights = await service.GetBookingsAsync(
                            flightId: flightId,
                            cancellationToken: cancellationToken
                    );
                    
                    return Results.Ok(flights);
                }).AddEndpointFilter<EndpointValidatorFilter<GetBookingsRequest>>();
    }
}