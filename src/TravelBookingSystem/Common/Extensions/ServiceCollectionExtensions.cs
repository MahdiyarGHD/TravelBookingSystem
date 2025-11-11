using System.Reflection;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using TravelBookingSystem.Common.Persistence;

namespace TravelBookingSystem.Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection ConfigureDbContexts(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TravelBookingDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString(TravelBookingDbContextSchema.DefaultConnectionStringName));
        });

        services.AddDbContext<TravelBookingDbContextReadOnly>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString(TravelBookingDbContextSchema.DefaultConnectionStringName))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        return services;
    }
    
    public static IServiceCollection ConfigureRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis")
                                    ?? throw new InvalidOperationException("Redis connection string 'Redis' not found.");
        
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));
        return services;
    }

    public static IServiceCollection ConfigureValidator(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(assembly: typeof(IAssemblyMarker).Assembly);

        return services;
    }
}
