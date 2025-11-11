using Carter;
using EasyMicroservices.ServiceContracts;
using Microsoft.AspNetCore.Mvc;
using TravelBookingSystem.Common.Filters;
using TravelBookingSystem.Features.Booking.Common;
using TravelBookingSystem.Features.Flight.Common;

namespace TravelBookingSystem.Features.Booking.BookSeat;

public class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app
            .MapGroup(FeatureManager.Prefix)
            .WithTags(FeatureManager.EndpointTagName)
            .MapPost("/",
                async ([FromBody] BookSeatRequest request, BookingService service,
                    CancellationToken cancellationToken) =>
                {
                    var flightId = Guid.Parse(request.FlightId);
                    var passengerId = Guid.Parse(request.PassengerId);
                    
                    var bookingResult = await service.BookSeatAsync(
                        flightId: flightId,
                        passengerId: passengerId,
                        cancellationToken: cancellationToken
                    );

                    if (!bookingResult)
                        return Results.BadRequest(bookingResult.ToContract<string>());
                    
                    return Results.Ok(bookingResult);
                })
            .AddEndpointFilter<EndpointValidatorFilter<BookSeatRequest>>()
            .Produces<MessageContract<string>>();
    }
}