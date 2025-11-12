using EasyMicroservices.ServiceContracts;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TravelBookingSystem.Common.Persistence;
using TravelBookingSystem.Features.Passenger.Common;
using TravelBookingSystem.Tests.Helpers;
using Xunit;

namespace TravelBookingSystem.Tests.Features;

public class PassengerServiceTests : IDisposable
{
    private readonly TravelBookingDbContext _dbContext;
    private readonly TravelBookingDbContextReadOnly _readOnlyDbContext;
    private readonly PassengerService _passengerService;

    public PassengerServiceTests()
    {
        (_dbContext, _readOnlyDbContext) = DbContextHelper.CreateInMemoryContexts();
        _passengerService = new PassengerService(_dbContext, _readOnlyDbContext);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreatePassenger()
    {
        // Arrange
        var fullName = "John Doe";
        var email = "john@example.com";
        var passportNumber = "ABC123456";
        var phoneNumber = "+1234567890";

        // Act
        var result = await _passengerService.CreateAsync(fullName, email, passportNumber, phoneNumber);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Result.Should().NotBeEmpty();

        var passenger = await _readOnlyDbContext.Passengers.FirstOrDefaultAsync(p => p.Id == result.Result);
        passenger.Should().NotBeNull();
        passenger!.FullName.Should().Be(fullName);
        passenger.Email.Should().Be(email);
        passenger.PassportNumber.Should().Be(passportNumber);
        passenger.PhoneNumber.Should().Be(phoneNumber);
    }

    [Fact]
    public async Task CreateAsync_WithNullPhoneNumber_ShouldCreatePassenger()
    {
        // Arrange
        var fullName = "John Doe";
        var email = "john@example.com";
        var passportNumber = "ABC123456";

        // Act
        var result = await _passengerService.CreateAsync(fullName, email, passportNumber, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Result.Should().NotBeEmpty();

        var passenger = await _readOnlyDbContext.Passengers.FirstOrDefaultAsync(p => p.Id == result.Result);
        passenger.Should().NotBeNull();
        passenger!.PhoneNumber.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_WithDuplicatePhoneNumber_ShouldReturnError()
    {
        // Arrange
        var phoneNumber = "+1234567890";
        var existingPassenger = Passenger.Create("Jane Doe", "jane@example.com", "XYZ789", phoneNumber);
        _dbContext.Passengers.Add(existingPassenger);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _passengerService.CreateAsync(
            "John Doe", "john@example.com", "ABC123", phoneNumber);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.FailedReasonType.Should().Be(FailedReasonType.Incorrect);
        result.Error.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateAsync_WithDifferentPhoneNumbers_ShouldCreateBothPassengers()
    {
        // Arrange
        var passenger1Data = ("John Doe", "john@example.com", "ABC123", "+1234567890");
        var passenger2Data = ("Jane Smith", "jane@example.com", "XYZ789", "+0987654321");

        // Act
        var result1 = await _passengerService.CreateAsync(
            passenger1Data.Item1, passenger1Data.Item2, passenger1Data.Item3, passenger1Data.Item4);
        var result2 = await _passengerService.CreateAsync(
            passenger2Data.Item1, passenger2Data.Item2, passenger2Data.Item3, passenger2Data.Item4);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Result.Should().NotBe(result2.Result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllPassengers()
    {
        // Arrange
        var passenger1 = Passenger.Create("John Doe", "john@example.com", "ABC123", "+1234567890");
        var passenger2 = Passenger.Create("Jane Smith", "jane@example.com", "XYZ789", "+0987654321");
        
        _dbContext.Passengers.AddRange(passenger1, passenger2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _passengerService.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.FullName == "John Doe");
        result.Should().Contain(p => p.FullName == "Jane Smith");
    }

    [Fact]
    public async Task GetAllAsync_WithNoPassengers_ShouldReturnEmptyList()
    {
        // Act
        var result = await _passengerService.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("John Doe", "john@example.com", "ABC123456", "+1234567890")]
    [InlineData("Jane Smith", "jane.smith@test.com", "XYZ987654", "+9876543210")]
    [InlineData("Sponge Bob", "sponge@bob.org", "DEF111222", null)]
    public async Task CreateAsync_WithVariousValidInputs_ShouldSucceed(
        string fullName, string email, string passportNumber, string? phoneNumber)
    {
        // Act
        var result = await _passengerService.CreateAsync(fullName, email, passportNumber, phoneNumber);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        var passenger = await _readOnlyDbContext.Passengers.FirstOrDefaultAsync(p => p.Id == result.Result);
        passenger.Should().NotBeNull();
        passenger!.FullName.Should().Be(fullName);
        passenger.Email.Should().Be(email);
        passenger.PassportNumber.Should().Be(passportNumber);
        passenger.PhoneNumber.Should().Be(phoneNumber);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
        _readOnlyDbContext.Dispose();
    }
}
