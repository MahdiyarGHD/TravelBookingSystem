using Carter;
using EasyMicroservices.ServiceContracts;
using Microsoft.AspNetCore.Mvc;
using TravelBookingSystem.Common.Filters;
using TravelBookingSystem.Features.Flight.Common;

namespace TravelBookingSystem.Features.Flight.Create;

public class Endpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app
            .MapGroup(FeatureManager.Prefix)
            .WithTags(FeatureManager.EndpointTagName)
            .MapPost("/",
                async ([FromBody] CreateFlightRequest request, FlightService service,
                        CancellationToken cancellationToken) =>
                    {
                        var flightResult = await service.CreateAsync(
                            destination: request.Destination,
                            origin: request.Origin,
                            availableSeats: request.AvailableSeats,
                            flightNumber: request.FlightNumber,
                            price: request.Price,
                            arrivalDate: request.ArrivalDate,
                            departureDate: request.DepartureDate,
                            cancellationToken: cancellationToken
                        );

                        if (!flightResult)
                            return Results.BadRequest(flightResult.ToContract<CreateFlightResponse>());

                        return Results.Ok((MessageContract<string>)flightResult.Result.ToString());
                    }
            )
            .AddEndpointFilter<EndpointValidatorFilter<CreateFlightRequest>>()
            .Produces<MessageContract<string>>();
    }
}