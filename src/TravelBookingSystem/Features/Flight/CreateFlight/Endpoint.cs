using Carter;
using Microsoft.AspNetCore.Mvc;
using TravelBookingSystem.Common.Filters;
using TravelBookingSystem.Features.Flight.Common;

namespace TravelBookingSystem.Features.Flight.CreateFlight;

public class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app
            .MapGroup(FeatureManager.Prefix)
            .WithTags(FeatureManager.EndpointTagName)
            .MapPost("/",
                async ([FromBody] CreateFlightRequest request, FlightService service, CancellationToken cancellationToken) =>
                {
                    var flightId = await service.CreateAsync(
                        destination: request.Destination,
                        origin: request.Origin,
                        availableSeats: request.AvailableSeats,
                        flightNumber: request.FlightNumber,
                        price: request.Price,
                        arrivalDate: request.ArrivalDate,
                        departureDate: request.DepartureDate
                        );
                    
                    return new CreateFlightResponse(flightId.ToString());
                }).AddEndpointFilter<EndpointValidatorFilter<CreateFlightRequest>>();
    }
}