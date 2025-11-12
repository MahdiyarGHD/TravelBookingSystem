using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using TravelBookingSystem.Common.Persistence;
using TravelBookingSystem.Features.Flight.Common;
using TravelBookingSystem.Features.Passenger.Common;

namespace TravelBookingSystem.IntegrationTests;

public class BookingIntegrationTests(IntegrationTestFixture fixture) : IClassFixture<IntegrationTestFixture>
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task BookSeat_WithRealInfrastructure_ShouldSucceed()
    {
        var passengerRequest = new
        {
            fullName = "John Doe",
            email = "john@example.com",
            passportNumber = "ABC123456",
            phoneNumber = "+1234567890"
        };

        var passengerResponse = await _client!.PostAsJsonAsync("/passengers", passengerRequest);
        passengerResponse.EnsureSuccessStatusCode();
        
        var passengerResult = await passengerResponse.Content.ReadFromJsonAsync<MessageResponse>();
        var passengerId = Guid.Parse(passengerResult!.Result!);

        var flightRequest = new
        {
            flightNumber = "FL001",
            origin = "TRN",
            destination = "MHD",
            departureDate = DateTimeOffset.UtcNow.AddDays(1),
            arrivalDate = DateTimeOffset.UtcNow.AddDays(1).AddHours(5),
            availableSeats = 100,
            price = 299.99m
        };

        var flightResponse = await _client!.PostAsJsonAsync("/flights", flightRequest);
        flightResponse.EnsureSuccessStatusCode();
        
        var flightResult = await flightResponse.Content.ReadFromJsonAsync<MessageResponse>();
        var flightId = Guid.Parse(flightResult!.Result!);

        var bookingRequest = new
        {
            passengerId,
            flightId
        };

        var bookingResponse = await _client!.PostAsJsonAsync("/bookings", bookingRequest);

        // Assert
        bookingResponse.EnsureSuccessStatusCode();
        var bookingResult = await bookingResponse.Content.ReadFromJsonAsync<MessageResponse>();
        bookingResult.Should().NotBeNull();
        bookingResult!.IsSuccess.Should().BeTrue();

        // Verify in database
        var readContext = fixture.GetDbContext<TravelBookingDbContextReadOnly>();
        var booking = await readContext.Bookings
            .FirstOrDefaultAsync(b => b.PassengerId == passengerId && b.FlightId == flightId);

        booking.Should().NotBeNull();
        booking!.SeatNumber.Should().Be(1);
    }

    [Fact]
    public async Task BookSeat_ConcurrentRequestsWithRealRedis_ShouldAllocateUniqueSeats()
    {
        // Arrange
        var writeContext = fixture.GetDbContext<TravelBookingDbContext>();
        var readContext = fixture.GetDbContext<TravelBookingDbContextReadOnly>();

        var passengers = Enumerable.Range(1, 5)
            .Select(i => Passenger.Create(
                $"Passenger {i}",
                $"passenger{i}@example.com",
                $"PASS{i}",
                $"+123456789{i}"))
            .ToList();

        var flight = Flight.Create(
            "FL002",
            "SFO",
            "SEA",
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
            100,
            199.99m);

        writeContext.Passengers.AddRange(passengers);
        writeContext.Flights.Add(flight);
        await writeContext.SaveChangesAsync();

        // Act
        var bookingTasks = passengers.Select(p => Task.Run(async () =>
        {
            var bookingRequest = new { passengerId = p.Id, flightId = flight.Id };
            var response = await _client!.PostAsJsonAsync("/bookings", bookingRequest);
            return await response.Content.ReadFromJsonAsync<MessageResponse>();
        })).ToArray();

        var results = await Task.WhenAll(bookingTasks);

        // Assert
        results.Should().AllSatisfy(r =>
        {
            r.Should().NotBeNull();
            r!.IsSuccess.Should().BeTrue();
        });

        // Verify seat numbers are unique
        var bookings = await readContext.Bookings
            .Where(b => b.FlightId == flight.Id)
            .ToListAsync();

        bookings.Should().HaveCount(5);
        var seatNumbers = bookings.Select(b => b.SeatNumber).ToList();
        seatNumbers.Distinct().Should().HaveCount(5, "all seats should be unique");
        seatNumbers.Should().OnlyContain(s => s >= 1 && s <= 5);
    }

    [Fact]
    public async Task BookSeat_ConcurrentRequestsForLastSeat_OnlyOneSucceeds()
    {
        // Arrange
        var writeContext = fixture.GetDbContext<TravelBookingDbContext>();
        var readContext = fixture.GetDbContext<TravelBookingDbContextReadOnly>();
        
        var passengers = Enumerable.Range(1, 3)
            .Select(i => Passenger.Create(
                $"Passenger {i}",
                $"lastSeat{i}@example.com",
                $"LS{i}",
                $"+999888777{i}"))
            .ToList();

        var flight = Flight.Create(
            "FL003",
            "MHD",
            "TRN",
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(1).AddHours(6),
            1, // Only 1 seat
            399.99m);

        writeContext.Passengers.AddRange(passengers);
        writeContext.Flights.Add(flight);
        await writeContext.SaveChangesAsync();

        // Act 
        var bookingTasks = passengers.Select(p => Task.Run(async () =>
        {
            try
            {
                var bookingRequest = new { passengerId = p.Id, flightId = flight.Id };
                var response = await _client!.PostAsJsonAsync("/bookings", bookingRequest);
                var content = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<MessageResponse>();
                }
                
                return new MessageResponse { IsSuccess = false };
            }
            catch
            {
                return new MessageResponse { IsSuccess = false };
            }
        })).ToArray();

        var results = await Task.WhenAll(bookingTasks);

        var successCount = results.Count(r => r?.IsSuccess == true);
        successCount.Should().Be(1, "only one passenger should get the last seat");

        // Verify only 1 booking exists
        var bookings = await readContext.Bookings
            .Where(b => b.FlightId == flight.Id)
            .ToListAsync();

        bookings.Should().HaveCount(1);
        bookings[0].SeatNumber.Should().Be(1);
    }

    [Fact]
    public async Task FlightFilter_WithRealDatabase_ShouldReturnCorrectResults()
    {
        // Arrange 
        var writeContext = fixture.GetDbContext<TravelBookingDbContext>();
        
        var flights = new[]
        {
            Flight.Create("FL100", "MHD", "TRN", DateTimeOffset.UtcNow.AddDays(1), 
                DateTimeOffset.UtcNow.AddDays(1).AddHours(5), 100, 299.99m),
            Flight.Create("FL101", "MHD", "ISF", DateTimeOffset.UtcNow.AddDays(1), 
                DateTimeOffset.UtcNow.AddDays(1).AddHours(4), 150, 249.99m),
            Flight.Create("FL102", "ISF", "TRN", DateTimeOffset.UtcNow.AddDays(2), 
                DateTimeOffset.UtcNow.AddDays(2).AddHours(2), 80, 199.99m),
        };

        writeContext.Flights.AddRange(flights);
        await writeContext.SaveChangesAsync();

        // Act
        var response = await _client!.GetAsync("/flights/filter?origin=MHD");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<FlightListResponse>();
        
        result.Should().NotBeNull();
        result!.Result.Should().HaveCount(2);
        result.Result.Should().AllSatisfy(f => f.Origin.Should().Be("MHD"));
    }

    // DTOs for deserialization
    private class MessageResponse
    {
        public bool IsSuccess { get; set; }
        public string? Result { get; set; }
    }

    private class FlightListResponse
    {
        public bool IsSuccess { get; set; }
        public List<FlightDto> Result { get; set; } = [];
    }

    private class FlightDto
    {
        public Guid Id { get; set; }
        public string FlightNumber { get; set; } = "";
        public string Origin { get; set; } = "";
        public string Destination { get; set; } = "";
        public DateTimeOffset DepartureTime { get; set; }
        public DateTimeOffset ArrivalTime { get; set; }
        public int AvailableSeats { get; set; }
        public decimal Price { get; set; }
    }
}
