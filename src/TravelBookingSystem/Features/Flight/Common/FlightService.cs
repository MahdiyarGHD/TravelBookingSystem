using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using TravelBookingSystem.Common.Persistence;
using TravelBookingSystem.Features.Booking.Common;

namespace TravelBookingSystem.Features.Flight.Common;

public class FlightService(TravelBookingDbContext dbContext)
{
    private readonly TravelBookingDbContext _dbContext = dbContext;

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
        
        var exists = await _dbContext.Flights
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
        var query = _dbContext.Flights.AsQueryable();

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
        var flight = await _dbContext.Flights.FindAsync(flightId, cancellationToken);
        if (flight is null) 
            throw new InvalidOperationException("Flight not found");

        if (newCapacity < 0) 
            throw new ArgumentException("Capacity must be non-negative");

        // we will check bookings here to not exceed the new capacity after implementing it

        flight.UpdateAvailableSeats(newCapacity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BookingDto>> GetBookingsAsync(
        Guid flightId,
        CancellationToken cancellationToken = default)
    {
        var flightExists = await _dbContext.Flights
            .AsNoTracking()
            .AnyAsync(f => f.Id == flightId, cancellationToken);

        if (!flightExists)
            throw new FlightNotFoundException(flightId);

        var bookings = await _dbContext.Bookings
            .AsNoTracking()
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