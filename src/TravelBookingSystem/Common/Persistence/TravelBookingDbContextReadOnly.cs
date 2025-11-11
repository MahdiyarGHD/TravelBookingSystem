
using Microsoft.EntityFrameworkCore;
using TravelBookingSystem.Features.Booking.Common;
using TravelBookingSystem.Features.Flight.Common;
using TravelBookingSystem.Features.Passenger.Common;

namespace TravelBookingSystem.Common.Persistence;

public class TravelBookingDbContextReadOnly : DbContext
{
    public TravelBookingDbContextReadOnly(DbContextOptions<TravelBookingDbContextReadOnly> dbContextOptions) : base(dbContextOptions)
    {

    }
    
    public IQueryable<Flight> Flights => Set<Flight>().AsQueryable();
    public IQueryable<Passenger> Passengers => Set<Passenger>().AsQueryable();
    public IQueryable<Booking> Bookings => Set<Booking>().AsQueryable();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(TravelBookingDbContextSchema.DefaultSchema);

        var assembly = typeof(IAssemblyMarker).Assembly;
        modelBuilder.ApplyConfigurationsFromAssembly(assembly);
    }
    
    public override int SaveChanges()
    {
        throw new InvalidOperationException("This is a read-only context.");
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("This is a read-only context.");
    }
}