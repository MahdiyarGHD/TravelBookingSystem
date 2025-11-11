using System.Collections.Immutable;
using Medallion.Threading;
using Microsoft.EntityFrameworkCore;
using TravelBookingSystem.Common.Persistence;
using TravelBookingSystem.Features.Booking.Common;

namespace TravelBookingSystem.Features.Flight.Common;

public class FlightService(    
    TravelBookingDbContext dbContext,
    TravelBookingDbContextReadOnly readonlyDbContext,
    IDistributedLockProvider distributedLockProvider)
{
    private readonly TravelBookingDbContext _dbContext = dbContext;
    private readonly TravelBookingDbContextReadOnly _readOnlyDbContext = readonlyDbContext;
    private readonly IDistributedLockProvider _distributedLockProvider = distributedLockProvider;

    public async Task<Guid> CreateAsync(
        int availableSeats, 
        string flightNumber, 
        decimal price, 
        string destination, 
        string origin, 
        DateTimeOffset departureDate, 
        DateTimeOffset arrivalDate,
        CancellationToken cancellationToken = default)
    {
        if (price < 0) 
            throw new ArgumentException("Price cannot be negative");
        
        if (availableSeats < 0)
            throw new ArgumentException("available seats cannot be negative");

        if (departureDate >= arrivalDate) 
            throw new ArgumentException("Departure must be before arrival");
        
        var exists = await _readOnlyDbContext.Flights
            .AnyAsync(f => f.FlightNumber == flightNumber, cancellationToken: cancellationToken);

        if (exists)
            throw new InvalidOperationException("Flight number already exists");
        
        var flight = Flight.Create(
            flightNumber,
            origin,
            destination,
            departureDate,
            arrivalDate,
            availableSeats,
            price
        );

        _dbContext.Flights.Add(flight);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return flight.Id;
    }

    public async Task<List<Flight>> FilterAsync(
        string? origin,
        string? destination,
        DateTimeOffset? departureDate,
        DateTimeOffset? arrivalDate,
        CancellationToken cancellationToken = default)
    {
        var query = _readOnlyDbContext.Flights;

        if (!string.IsNullOrWhiteSpace(origin))
            query = query.Where(f => f.Origin == origin);

        if (!string.IsNullOrWhiteSpace(destination))
            query = query.Where(f => f.Destination == destination);

        if (departureDate.HasValue)
            query = query.Where(f => f.DepartureTime.Date == departureDate.Value.Date);

        if (arrivalDate.HasValue)
            query = query.Where(f => f.ArrivalTime.Date == arrivalDate.Value.Date);

        return await query.ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task UpdateAvailableSeatsAsync(Guid flightId, int newCapacity, CancellationToken cancellationToken = default)
    {
        var @lock = _distributedLockProvider.CreateLock($"flight:{flightId}");
        await using var handle = await @lock.TryAcquireAsync(cancellationToken: cancellationToken);

        if (handle is null)
            throw new InvalidOperationException("Could not acquire lock for updating flight capacity. Please retry.");

        var flight = await _dbContext.Flights.FindAsync(flightId, cancellationToken);
        if (flight is null)
            throw new InvalidOperationException("Flight not found");

        var bookedCount = await _readOnlyDbContext.Bookings.CountAsync(b => b.FlightId == flightId, cancellationToken);
        if (bookedCount > newCapacity)
            throw new ArgumentException("Capacity must be more than the number of booked flights");

        flight.UpdateAvailableSeats(newCapacity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BookingDto>> GetBookingsAsync(
        Guid flightId,
        CancellationToken cancellationToken = default)
    {
        var flightExists = await _readOnlyDbContext.Flights
            .AnyAsync(f => f.Id == flightId, cancellationToken);

        if (!flightExists)
            throw new FlightNotFoundException(flightId);

        var bookings = await _readOnlyDbContext.Bookings
            .Where(b => b.FlightId == flightId)
            .Select(b => new BookingDto(
                b.Id,
                b.PassengerId,
                b.Passenger.FullName,
                b.FlightId,
                b.SeatNumber,
                b.BookingDate
            ))
            .ToListAsync(cancellationToken);

        return bookings;
    }
}