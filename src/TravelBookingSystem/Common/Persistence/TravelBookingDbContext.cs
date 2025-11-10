
using Microsoft.EntityFrameworkCore;
using TravelBookingSystem.Features.Flight.Common;

namespace TravelBookingSystem.Common.Persistence;

public class TravelBookingDbContext : DbContext
{
    public TravelBookingDbContext(DbContextOptions<TravelBookingDbContext> dbContextOptions) : base(dbContextOptions)
    {

    }
    
    public DbSet<Flight> Flights => Set<Flight>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(TravelBookingDbContextSchema.DefaultSchema);

        var assembly = typeof(IAssemblyMarker).Assembly;
        modelBuilder.ApplyConfigurationsFromAssembly(assembly);
    }
}