using Medallion.Threading;
using Medallion.Threading.Redis;
using StackExchange.Redis;

namespace TravelBookingSystem.Common.Providers;

public class RedisDistributedLockProvider(IConnectionMultiplexer connection) 
    : IDistributedLockProvider
{
    private readonly IDatabase _database = connection.GetDatabase();

    public IDistributedLock CreateLock(string name)
    {
        return new RedisDistributedLock(name, _database);
    }
}