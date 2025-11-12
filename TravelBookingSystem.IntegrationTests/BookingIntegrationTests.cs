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

public class BookingIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("travelbooking_test")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .WithCleanUp(true)
        .Build();
    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .WithCleanUp(true)
        .Build();
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;
    private IServiceScope? _scope;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _postgresContainer.StartAsync(),
            _redisContainer.StartAsync()
        );

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptors = services.Where(d =>
                        d.ServiceType == typeof(DbContextOptions<TravelBookingDbContext>) ||
                        d.ServiceType == typeof(DbContextOptions<TravelBookingDbContextReadOnly>) ||
                        d.ServiceType == typeof(TravelBookingDbContext) ||
                        d.ServiceType == typeof(TravelBookingDbContextReadOnly) ||
                        d.ServiceType == typeof(IConnectionMultiplexer))
                        .ToList();

                    foreach (var descriptor in descriptors)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<TravelBookingDbContext>(options =>
                        options.UseNpgsql(_postgresContainer.GetConnectionString()));

                    services.AddDbContext<TravelBookingDbContextReadOnly>(options =>
                        options.UseNpgsql(_postgresContainer.GetConnectionString()));

                    services.AddSingleton<IConnectionMultiplexer>(sp =>
                        ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString()));
                });
            });

        _client = _factory.CreateClient();

        // Apply migrations
        _scope = _factory.Services.CreateScope();
        var dbContext = _scope.ServiceProvider.GetRequiredService<TravelBookingDbContext>();
        await dbContext.Database.MigrateAsync();
    }

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
        var dbContext = _scope!.ServiceProvider.GetRequiredService<TravelBookingDbContextReadOnly>();
        var booking = await dbContext.Bookings
            .FirstOrDefaultAsync(b => b.PassengerId == passengerId && b.FlightId == flightId);

        booking.Should().NotBeNull();
        booking!.SeatNumber.Should().Be(1);
    }

    [Fact]
    public async Task BookSeat_ConcurrentRequestsWithRealRedis_ShouldAllocateUniqueSeats()
    {
        // Arrange
        var dbContext = _scope!.ServiceProvider.GetRequiredService<TravelBookingDbContext>();
        
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

        dbContext.Passengers.AddRange(passengers);
        dbContext.Flights.Add(flight);
        await dbContext.SaveChangesAsync();

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
        var readContext = _scope.ServiceProvider.GetRequiredService<TravelBookingDbContextReadOnly>();
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
        var dbContext = _scope!.ServiceProvider.GetRequiredService<TravelBookingDbContext>();
        
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

        dbContext.Passengers.AddRange(passengers);
        dbContext.Flights.Add(flight);
        await dbContext.SaveChangesAsync();

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
        var readContext = _scope.ServiceProvider.GetRequiredService<TravelBookingDbContextReadOnly>();
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
        var dbContext = _scope!.ServiceProvider.GetRequiredService<TravelBookingDbContext>();
        
        var flights = new[]
        {
            Flight.Create("FL100", "MHD", "TRN", DateTimeOffset.UtcNow.AddDays(1), 
                DateTimeOffset.UtcNow.AddDays(1).AddHours(5), 100, 299.99m),
            Flight.Create("FL101", "MHD", "ISF", DateTimeOffset.UtcNow.AddDays(1), 
                DateTimeOffset.UtcNow.AddDays(1).AddHours(4), 150, 249.99m),
            Flight.Create("FL102", "ISF", "TRN", DateTimeOffset.UtcNow.AddDays(2), 
                DateTimeOffset.UtcNow.AddDays(2).AddHours(2), 80, 199.99m),
        };

        dbContext.Flights.AddRange(flights);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await _client!.GetAsync("/flights/filter?origin=MHD");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<FlightListResponse>();
        
        result.Should().NotBeNull();
        result!.Result.Should().HaveCount(2);
        result.Result.Should().AllSatisfy(f => f.Origin.Should().Be("MHD"));
    }

    public async Task DisposeAsync()
    {
        _scope?.Dispose();
        _client?.Dispose();
        _factory?.Dispose();
        
        await Task.WhenAll(
            _postgresContainer.StopAsync(),
            _redisContainer.StopAsync()
        );
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
