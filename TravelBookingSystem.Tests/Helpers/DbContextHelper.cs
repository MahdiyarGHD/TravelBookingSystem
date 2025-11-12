using Microsoft.EntityFrameworkCore;
using TravelBookingSystem.Common.Persistence;

namespace TravelBookingSystem.Tests.Helpers;

public static class DbContextHelper
{
    public static (TravelBookingDbContext writeContext, TravelBookingDbContextReadOnly readContext) CreateInMemoryContexts()
    {
        var databaseName = Guid.NewGuid().ToString();
        
        var writeOptions = new DbContextOptionsBuilder<TravelBookingDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        var readOptions = new DbContextOptionsBuilder<TravelBookingDbContextReadOnly>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;

        var writeContext = new TravelBookingDbContext(writeOptions);
        var readContext = new TravelBookingDbContextReadOnly(readOptions);

        return (writeContext, readContext);
    }
}
