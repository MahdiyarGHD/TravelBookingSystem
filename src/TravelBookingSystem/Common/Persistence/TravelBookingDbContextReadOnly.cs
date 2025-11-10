
using Microsoft.EntityFrameworkCore;

namespace TravelBookingSystem.Common.Persistence;

public class TravelBookingDbContextReadOnly : DbContext
{
    public TravelBookingDbContextReadOnly(DbContextOptions<TravelBookingDbContextReadOnly> dbContextOptions) : base(dbContextOptions)
    {

    }
    
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