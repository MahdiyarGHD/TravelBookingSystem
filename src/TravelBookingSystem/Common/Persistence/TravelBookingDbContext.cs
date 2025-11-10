
using Microsoft.EntityFrameworkCore;

namespace TravelBookingSystem.Common.Persistence;

public class TravelBookingDbContext : DbContext
{
    public TravelBookingDbContext(DbContextOptions<TravelBookingDbContext> dbContextOptions) : base(dbContextOptions)
    {

    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(TravelBookingDbContextSchema.DefaultSchema);

        var assembly = typeof(IAssemblyMarker).Assembly;
        modelBuilder.ApplyConfigurationsFromAssembly(assembly);
    }
}