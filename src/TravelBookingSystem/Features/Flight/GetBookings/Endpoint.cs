using Carter;
using EasyMicroservices.ServiceContracts;
using Microsoft.AspNetCore.Mvc;
using TravelBookingSystem.Common.Filters;
using TravelBookingSystem.Features.Booking.Common;
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
                    
                    var flightsResult = await service.GetBookingsAsync(
                            flightId: flightId,
                            cancellationToken: cancellationToken
                    );
                    
                    if (!flightsResult)
                        return Results.BadRequest(flightsResult.ToListContract<BookingDto>());
                    
                    return Results.Ok(flightsResult);
                })
            .AddEndpointFilter<EndpointValidatorFilter<GetBookingsRequest>>()
            .Produces<ListMessageContract<BookingDto>>();
    }
}