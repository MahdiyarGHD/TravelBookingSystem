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

public class BookingServiceTests : IDisposable
{
    private readonly TravelBookingDbContext _dbContext;
    private readonly TravelBookingDbContextReadOnly _readOnlyDbContext;
    private readonly Mock<IDistributedLockProvider> _lockProviderMock;
    private readonly BookingService _bookingService;

    public BookingServiceTests()
    {
        (_dbContext, _readOnlyDbContext) = DbContextHelper.CreateInMemoryContexts();
        _lockProviderMock = new Mock<IDistributedLockProvider>();
        _bookingService = new BookingService(_dbContext, _readOnlyDbContext, _lockProviderMock.Object);
    }

    [Fact]
    public async Task BookSeatAsync_WithValidData_ShouldCreateBooking()
    {
        // Arrange
        var passenger = Passenger.Create("John Doe", "john@example.com", "ABC123", "+1234567890");
        var flight = Flight.Create("FL001", "TRN", "MHD", 
            DateTimeOffset.UtcNow.AddDays(1), 
            DateTimeOffset.UtcNow.AddDays(1).AddHours(5), 
            100, 299.99m);

        _dbContext.Passengers.Add(passenger);
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

        // Act
        var result = await _bookingService.BookSeatAsync(passenger.Id, flight.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Result.Should().NotBeEmpty();

        var booking = await _readOnlyDbContext.Bookings.FirstOrDefaultAsync(b => b.Id == result.Result);
        booking.Should().NotBeNull();
        booking!.PassengerId.Should().Be(passenger.Id);
        booking.FlightId.Should().Be(flight.Id);
        booking.SeatNumber.Should().Be(1);
    }

    [Fact]
    public async Task BookSeatAsync_WithNonExistentFlight_ShouldReturnError()
    {
        // Arrange
        var passenger = Passenger.Create("John Doe", "john@example.com", "ABC123", "+1234567890");
        _dbContext.Passengers.Add(passenger);
        await _dbContext.SaveChangesAsync();

        var nonExistentFlightId = Guid.CreateVersion7();

        // Act
        var result = await _bookingService.BookSeatAsync(passenger.Id, nonExistentFlightId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.FailedReasonType.Should().Be(FailedReasonType.Incorrect);
        result.Error.Message.Should().Contain("was not found");
    }

    [Fact]
    public async Task BookSeatAsync_WithNonExistentPassenger_ShouldReturnError()
    {
        // Arrange
        var flight = Flight.Create("FL001", "TRN", "MHD", 
            DateTimeOffset.UtcNow.AddDays(1), 
            DateTimeOffset.UtcNow.AddDays(1).AddHours(5), 
            100, 299.99m);
        _dbContext.Flights.Add(flight);
        await _dbContext.SaveChangesAsync();

        var nonExistentPassengerId = Guid.CreateVersion7();

        // Act
        var result = await _bookingService.BookSeatAsync(nonExistentPassengerId, flight.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.FailedReasonType.Should().Be(FailedReasonType.Incorrect);
        result.Error.Message.Should().Contain("was not found");
    }

    [Fact]
    public async Task BookSeatAsync_WhenPassengerAlreadyBooked_ShouldReturnError()
    {
        // Arrange
        var passenger = Passenger.Create("John Doe", "john@example.com", "ABC123", "+1234567890");
        var flight = Flight.Create("FL001", "TRN", "MHD", 
            DateTimeOffset.UtcNow.AddDays(1), 
            DateTimeOffset.UtcNow.AddDays(1).AddHours(5), 
            100, 299.99m);

        var existingBooking = Booking.Create(passenger.Id, flight.Id, 1, DateTimeOffset.UtcNow);

        _dbContext.Passengers.Add(passenger);
        _dbContext.Flights.Add(flight);
        _dbContext.Bookings.Add(existingBooking);
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
        var result = await _bookingService.BookSeatAsync(passenger.Id, flight.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.FailedReasonType.Should().Be(FailedReasonType.Duplicate);
        result.Error.Message.Should().Contain("already booked");
    }

    [Fact]
    public async Task BookSeatAsync_WhenFlightIsFull_ShouldReturnError()
    {
        // Arrange
        var passenger1 = Passenger.Create("John Doe", "john@example.com", "ABC123", "+1234567890");
        var passenger2 = Passenger.Create("Jane Smith", "jane@example.com", "XYZ789", "+0987654321");
        var flight = Flight.Create("FL001", "TRN", "MHD", 
            DateTimeOffset.UtcNow.AddDays(1), 
            DateTimeOffset.UtcNow.AddDays(1).AddHours(5), 
            1, 299.99m); // Only 1 seat

        var existingBooking = Booking.Create(passenger1.Id, flight.Id, 1, DateTimeOffset.UtcNow);

        _dbContext.Passengers.AddRange(passenger1, passenger2);
        _dbContext.Flights.Add(flight);
        _dbContext.Bookings.Add(existingBooking);
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
        var result = await _bookingService.BookSeatAsync(passenger2.Id, flight.Id);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.FailedReasonType.Should().Be(FailedReasonType.Incorrect);
        result.Error.Message.Should().Contain("doesn't have any available seats");
    }

    [Fact]
    public async Task BookSeatAsync_ShouldAllocateSequentialSeats()
    {
        // Arrange
        var passenger1 = Passenger.Create("John Doe", "john@example.com", "ABC123", "+1234567890");
        var passenger2 = Passenger.Create("Jane Smith", "jane@example.com", "XYZ789", "+0987654321");
        var flight = Flight.Create("FL001", "TRN", "MHD", 
            DateTimeOffset.UtcNow.AddDays(1), 
            DateTimeOffset.UtcNow.AddDays(1).AddHours(5), 
            100, 299.99m);

        _dbContext.Passengers.AddRange(passenger1, passenger2);
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

        // Act
        var result1 = await _bookingService.BookSeatAsync(passenger1.Id, flight.Id);
        var result2 = await _bookingService.BookSeatAsync(passenger2.Id, flight.Id);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();

        var booking1 = await _readOnlyDbContext.Bookings.FirstOrDefaultAsync(b => b.Id == result1.Result);
        var booking2 = await _readOnlyDbContext.Bookings.FirstOrDefaultAsync(b => b.Id == result2.Result);

        booking1!.SeatNumber.Should().Be(1);
        booking2!.SeatNumber.Should().Be(2);
    }

    [Fact]
    public async Task BookSeatAsync_WhenLockNotAcquired_ShouldThrowException()
    {
        // Arrange
        var passenger = Passenger.Create("John Doe", "john@example.com", "ABC123", "+1234567890");
        var flight = Flight.Create("FL001", "TRN", "MHD", 
            DateTimeOffset.UtcNow.AddDays(1), 
            DateTimeOffset.UtcNow.AddDays(1).AddHours(5), 
            100, 299.99m);

        _dbContext.Passengers.Add(passenger);
        _dbContext.Flights.Add(flight);
        await _dbContext.SaveChangesAsync();

        var distributedLockMock = new Mock<IDistributedLock>();
        distributedLockMock
            .Setup(x => x.TryAcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDistributedSynchronizationHandle?)null);

        _lockProviderMock
            .Setup(x => x.CreateLock(It.IsAny<string>()))
            .Returns(distributedLockMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<BookingLockUnavailableException>(
            () => _bookingService.BookSeatAsync(passenger.Id, flight.Id));
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
        _readOnlyDbContext.Dispose();
    }
}
