using Microsoft.EntityFrameworkCore;
using TravelBookingSystem.Common.Persistence;

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
            .AnyAsync(f => f.FlightNumber == flightNumber);

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
        await _dbContext.SaveChangesAsync();

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

        return await query.ToListAsync();
    }

    public async Task UpdateAvailableSeatsAsync(Guid flightId, int newCapacity, CancellationToken cancellationToken = default)
    {
        var flight = await _dbContext.Flights.FindAsync(flightId);
        if (flight is null) 
            throw new InvalidOperationException("Flight not found");

        if (newCapacity < 0) 
            throw new ArgumentException("Capacity must be non-negative");

        // we will check bookings here to not exceed the new capacity after implementing it

        flight.AvailableSeats = newCapacity;
        await _dbContext.SaveChangesAsync();
    }
}