using EasyMicroservices.ServiceContracts;
using FluentAssertions;
using Medallion.Threading;
using Microsoft.EntityFrameworkCore;
using Moq;
using TravelBookingSystem.Common.Persistence;
using TravelBookingSystem.Features.Booking.Common;
using TravelBookingSystem.Features.Flight.Common;
using TravelBookingSystem.Features.Passenger.Common;
using TravelBookingSystem.Tests.Helpers;
using Xunit;

namespace TravelBookingSystem.Tests.Features;

public class FlightServiceTests : IDisposable
{
    private readonly TravelBookingDbContext _dbContext;
    private readonly TravelBookingDbContextReadOnly _readOnlyDbContext;
    private readonly Mock<IDistributedLockProvider> _lockProviderMock;
    private readonly FlightService _flightService;

    public FlightServiceTests()
    {
        (_dbContext, _readOnlyDbContext) = DbContextHelper.CreateInMemoryContexts();
        _lockProviderMock = new Mock<IDistributedLockProvider>();
        _flightService = new FlightService(_dbContext, _readOnlyDbContext, _lockProviderMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateFlight()
    {
        // Arrange
        var flightNumber = "FL001";
        var origin = "TRN";
        var destination = "MHD";
        var departureDate = DateTimeOffset.UtcNow.AddDays(1);
        var arrivalDate = departureDate.AddHours(5);
        var availableSeats = 100;
        var price = 299.99m;

        // Act
        var result = await _flightService.CreateAsync(
            availableSeats, flightNumber, price, destination, origin, departureDate, arrivalDate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Result.Should().NotBeEmpty();

        var flight = await _readOnlyDbContext.Flights.FirstOrDefaultAsync(f => f.Id == result.Result);
        flight.Should().NotBeNull();
        flight!.FlightNumber.Should().Be(flightNumber);
        flight.Origin.Should().Be(origin);
        flight.Destination.Should().Be(destination);
        flight.AvailableSeats.Should().Be(availableSeats);
        flight.Price.Should().Be(price);
    }

    [Fact]
    public async Task CreateAsync_WithNegativePrice_ShouldReturnError()
    {
        // Arrange
        var price = -100m;

        // Act
        var result = await _flightService.CreateAsync(
            100, "FL001", price, "TRN", "MHD", 
            DateTimeOffset.UtcNow.AddDays(1), 
            DateTimeOffset.UtcNow.AddDays(1).AddHours(5));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.FailedReasonType.Should().Be(FailedReasonType.Incorrect);
        result.Error.Message.Should().Contain("cannot be negative");
    }

    [Fact]
    public async Task CreateAsync_WithNegativeAvailableSeats_ShouldReturnError()
    {
        // Arrange
        var availableSeats = -10;

        // Act
        var result = await _flightService.CreateAsync(
            availableSeats, "FL001", 299.99m, "TRN", "MHD", 
            DateTimeOffset.UtcNow.AddDays(1), 
            DateTimeOffset.UtcNow.AddDays(1).AddHours(5));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.FailedReasonType.Should().Be(FailedReasonType.Incorrect);
        result.Error.Message.Should().Contain("cannot be negative");
    }

    [Fact]
    public async Task CreateAsync_WithDepartureDateAfterArrival_ShouldReturnError()
    {
        // Arrange
        var departureDate = DateTimeOffset.UtcNow.AddDays(1);
        var arrivalDate = departureDate.AddHours(-1); // Before departure

        // Act
        var result = await _flightService.CreateAsync(
            100, "FL001", 299.99m, "TRN", "MHD", departureDate, arrivalDate);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.FailedReasonType.Should().Be(FailedReasonType.Incorrect);
        result.Error.Message.Should().Contain("Departure must be before arrival");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateFlightNumber_ShouldReturnError()
    {
        // Arrange
        var flightNumber = "FL001";
        var flight = Flight.Create(flightNumber, "MHD", "TRN", 
            DateTimeOffset.UtcNow.AddDays(1), 
            DateTimeOffset.UtcNow.AddDays(1).AddHours(5), 
            100, 299.99m);
        _dbContext.Flights.Add(flight);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _flightService.CreateAsync(
            100, flightNumber, 299.99m, "TRN", "MHD", 
            DateTimeOffset.UtcNow.AddDays(2), 
            DateTimeOffset.UtcNow.AddDays(2).AddHours(2));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.FailedReasonType.Should().Be(FailedReasonType.Duplicate);
        result.Error.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task FilterAsync_WithNoFilters_ShouldReturnAllFlights()
    {
        // Arrange
        var flight1 = Flight.Create("FL001", "MHD", "TRN", 
            DateTimeOffset.UtcNow.AddDays(1), 
            DateTimeOffset.UtcNow.AddDays(1).AddHours(5), 
            100, 299.99m);
        var flight2 = Flight.Create("FL002", "TRN", "MHD", 
            DateTimeOffset.UtcNow.AddDays(2), 
            DateTimeOffset.UtcNow.AddDays(2).AddHours(2), 
            150, 199.99m);

        _dbContext.Flights.AddRange(flight1, flight2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _flightService.FilterAsync(null, null, null, null);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task FilterAsync_WithOriginFilter_ShouldReturnMatchingFlights()
    {
        // Arrange
        var flight1 = Flight.Create("FL001", "MHD", "TRN", 
            DateTimeOffset.UtcNow.AddDays(1), 
            DateTimeOffset.UtcNow.AddDays(1).AddHours(5), 
            100, 299.99m);
        var flight2 = Flight.Create("FL002", "ISF", "MHD", 
            DateTimeOffset.UtcNow.AddDays(2), 
            DateTimeOffset.UtcNow.AddDays(2).AddHours(2), 
            150, 199.99m);

        _dbContext.Flights.AddRange(flight1, flight2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _flightService.FilterAsync("MHD", null, null, null);

        // Assert
        result.Should().HaveCount(1);
        result.First().Origin.Should().Be("MHD");
    }

    [Fact]
    public async Task FilterAsync_WithDestinationFilter_ShouldReturnMatchingFlights()
    {
        // Arrange
        var flight1 = Flight.Create("FL001", "MHD", "TRN", 
            DateTimeOffset.UtcNow.AddDays(1), 
            DateTimeOffset.UtcNow.AddDays(1).AddHours(5), 
            100, 299.99m);
        var flight2 = Flight.Create("FL002", "ISF", "TRN", 
            DateTimeOffset.UtcNow.AddDays(2), 
            DateTimeOffset.UtcNow.AddDays(2).AddHours(2), 
            150, 199.99m);

        _dbContext.Flights.AddRange(flight1, flight2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _flightService.FilterAsync(null, "TRN", null, null);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(f => f.Destination == "TRN");
    }

    [Fact]
    public async Task FilterAsync_WithDepartureDateFilter_ShouldReturnMatchingFlights()
    {
        // Arrange
        var targetDate = DateTimeOffset.UtcNow.AddDays(1);
        var flight1 = Flight.Create("FL001", "MHD", "TRN", 
            targetDate, targetDate.AddHours(5), 100, 299.99m);
        var flight2 = Flight.Create("FL002", "TRN", "MHD", 
            DateTimeOffset.UtcNow.AddDays(2), 
            DateTimeOffset.UtcNow.AddDays(2).AddHours(2), 
            150, 199.99m);

        _dbContext.Flights.AddRange(flight1, flight2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _flightService.FilterAsync(null, null, targetDate, null);

        // Assert
        result.Should().HaveCount(1);
        result.First().DepartureTime.Date.Should().Be(targetDate.Date);
    }

    [Fact]
    public async Task UpdateAvailableSeatsAsync_WithValidData_ShouldUpdateSeats()
    {
        // Arrange
        var flight = Flight.Create("FL001", "TRN", "MHD", 
            DateTimeOffset.UtcNow.AddDays(1), 
            DateTimeOffset.UtcNow.AddDays(1).AddHours(5), 
            100, 299.99m);
        _dbContext.Flights.Add(flight);
        await _dbContext.SaveChangesAsync();

        var lockMock = new Mock<IDistributedSynchronizationHandle>();
        var distributedLockMock = new Mock<IDistributedLock>();
        distributedLockMock
            .Setup(x => x.TryAcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockMock.Object);

        _lockProviderMock
            .Setup(x => x.CreateLock(It.IsAny<string>()))
            .Returns(distributedLockMock.Object);

        var newCapacity = 150;

        // Act
        var result = await _flightService.UpdateAvailableSeatsAsync(flight.Id, newCapacity);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        var updatedFlight = await _readOnlyDbContext.Flights.FirstOrDefaultAsync(f => f.Id == flight.Id);
        updatedFlight!.AvailableSeats.Should().Be(newCapacity);
    }

    [Fact]
    public async Task UpdateAvailableSeatsAsync_WithNonExistentFlight_ShouldReturnError()
    {
        // Arrange
        var nonExistentFlightId = Guid.NewGuid();

        var lockMock = new Mock<IDistributedSynchronizationHandle>();
        var distributedLockMock = new Mock<IDistributedLock>();
        distributedLockMock
            .Setup(x => x.TryAcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockMock.Object);

        _lockProviderMock
            .Setup(x => x.CreateLock(It.IsAny<string>()))
            .Returns(distributedLockMock.Object);

        // Act
        var result = await _flightService.UpdateAvailableSeatsAsync(nonExistentFlightId, 100);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.FailedReasonType.Should().Be(FailedReasonType.NotFound);
    }

    [Fact]
    public async Task UpdateAvailableSeatsAsync_WhenReducingBelowBookedSeats_ShouldReturnError()
    {
        // Arrange
        var passenger = Passenger.Create("John Doe", "john@example.com", "ABC123", "+1234567890");
        var flight = Flight.Create("FL001", "TRN", "HD", 
            DateTimeOffset.UtcNow.AddDays(1), 
            DateTimeOffset.UtcNow.AddDays(1).AddHours(5), 
            100, 299.99m);
        var booking = Booking.Create(passenger.Id, flight.Id, 50, DateTimeOffset.UtcNow);

        _dbContext.Passengers.Add(passenger);
        _dbContext.Flights.Add(flight);
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync();

        var lockMock = new Mock<IDistributedSynchronizationHandle>();
        var distributedLockMock = new Mock<IDistributedLock>();
        distributedLockMock
            .Setup(x => x.TryAcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockMock.Object);

        _lockProviderMock
            .Setup(x => x.CreateLock(It.IsAny<string>()))
            .Returns(distributedLockMock.Object);

        // Act
        var result = await _flightService.UpdateAvailableSeatsAsync(flight.Id, 30); // Less than seat 50

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.FailedReasonType.Should().Be(FailedReasonType.Incorrect);
        result.Error.Message.Should().Contain("Cannot reduce capacity");
    }

    [Fact]
    public async Task GetBookingsAsync_WithValidFlightId_ShouldReturnBookings()
    {
        // Arrange
        var passenger = Passenger.Create("John Doe", "john@example.com", "ABC123", "+1234567890");
        var flight = Flight.Create("FL001", "MHD", "TRN", 
            DateTimeOffset.UtcNow.AddDays(1), 
            DateTimeOffset.UtcNow.AddDays(1).AddHours(5), 
            100, 299.99m);
        var booking = Booking.Create(passenger.Id, flight.Id, 1, DateTimeOffset.UtcNow);

        _dbContext.Passengers.Add(passenger);
        _dbContext.Flights.Add(flight);
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _flightService.GetBookingsAsync(flight.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Result.Should().HaveCount(1);
        result.Result.First().FlightId.Should().Be(flight.Id);
        result.Result.First().PassengerId.Should().Be(passenger.Id);
    }

    [Fact]
    public async Task GetBookingsAsync_WithNonExistentFlight_ShouldReturnError()
    {
        // Arrange
        var nonExistentFlightId = Guid.NewGuid();

        // Act
        var result = await _flightService.GetBookingsAsync(nonExistentFlightId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.FailedReasonType.Should().Be(FailedReasonType.NotFound);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
        _readOnlyDbContext.Dispose();
    }
}
