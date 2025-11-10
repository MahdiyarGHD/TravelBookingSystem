using System.Reflection;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
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

        return services;
    }

    public static IServiceCollection ConfigureValidator(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(assembly: typeof(IAssemblyMarker).Assembly);

        return services;
    }
}
