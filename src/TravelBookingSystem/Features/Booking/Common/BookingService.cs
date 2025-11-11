using Medallion.Threading;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using TravelBookingSystem.Common.Persistence;

namespace TravelBookingSystem.Features.Booking.Common;

public class BookingService(
    TravelBookingDbContext dbContext,
    IDistributedLockProvider distributedLockProvider)
{
    private readonly TravelBookingDbContext _dbContext = dbContext;
    private readonly IDistributedLockProvider _distributedLockProvider = distributedLockProvider;

    public async Task<Guid> BookSeatAsync(
        Guid passengerId, 
        Guid flightId, 
        CancellationToken cancellationToken = default)
    {
        await ValidateBookingRequestAsync(passengerId, flightId, cancellationToken);

        await using var lockHandle = await AcquireFlightLockAsync(flightId, cancellationToken);

        await EnsurePassengerHasNotBookedFlightAsync(passengerId, flightId, cancellationToken);

        var flight = await _dbContext.Flights
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == flightId, cancellationToken);
        
        var availableSeatNumber = await GetNextAvailableSeatAsync(flight!, cancellationToken);

        var booking = CreateBooking(passengerId, flightId, availableSeatNumber);

        await PersistBookingAsync(booking, cancellationToken);

        return booking.Id;
    }

    private async Task ValidateBookingRequestAsync(
        Guid passengerId, 
        Guid flightId, 
        CancellationToken cancellationToken)
    {
        var flightExists = await _dbContext.Flights
            .AnyAsync(f => f.Id == flightId, cancellationToken);

        if (!flightExists)
            throw new FlightNotFoundException(flightId);

        var passengerExists = await _dbContext.Passengers
            .AnyAsync(p => p.Id == passengerId, cancellationToken);

        if (!passengerExists)
            throw new PassengerNotFoundException(passengerId);
    }

    private async Task<IDistributedSynchronizationHandle> AcquireFlightLockAsync(
        Guid flightId, 
        CancellationToken cancellationToken)
    {
        var @lock = _distributedLockProvider.CreateLock($"flight:{flightId}");
        
        var handle = await @lock.TryAcquireAsync(
            timeout: TimeSpan.FromSeconds(30), 
            cancellationToken: cancellationToken);

        if (handle is null)
            throw new BookingLockUnavailableException(flightId);

        return handle;
    }

    private async Task EnsurePassengerHasNotBookedFlightAsync(
        Guid passengerId, 
        Guid flightId, 
        CancellationToken cancellationToken)
    {
        var hasAlreadyBooked = await _dbContext.Bookings
            .AnyAsync(
                b => b.FlightId == flightId && b.PassengerId == passengerId, 
                cancellationToken);

        if (hasAlreadyBooked)
            throw new DuplicateBookingException(passengerId, flightId);
    }

    private async Task<int> GetNextAvailableSeatAsync(
        Flight.Common.Flight flight, 
        CancellationToken cancellationToken)
    {
        var bookedSeats = await _dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.FlightId == flight.Id)
            .Select(b => b.SeatNumber)
            .ToHashSetAsync(cancellationToken);

        if (bookedSeats.Count >= flight.AvailableSeats)
            throw new NoSeatsAvailableException(flight.Id);

        var nextSeat = Enumerable.Range(1, flight.AvailableSeats)
            .FirstOrDefault(n => !bookedSeats.Contains(n));

        if (nextSeat == 0)
            throw new NoSeatsAvailableException(flight.Id);

        return nextSeat;
    }

    private static Booking CreateBooking(Guid passengerId, Guid flightId, int seatNumber)
    {
        return Booking.Create(
            passengerId,
            flightId,
            seatNumber,
            DateTimeOffset.UtcNow);
    }

    private async Task PersistBookingAsync(Booking booking, CancellationToken cancellationToken)
    {
        _dbContext.Bookings.Add(booking);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateSeatException(ex))
        {
            throw new DuplicateSeatNumberException(booking.FlightId, booking.SeatNumber, ex);
        }
    }

    private static bool IsDuplicateSeatException(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) ?? false;
    }
}