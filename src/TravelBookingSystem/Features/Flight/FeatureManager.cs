using ServiceCollector.Abstractions;
using TravelBookingSystem.Features.Flight.Common;

namespace TravelBookingSystem.Features.Flight;

public abstract class FeatureManager
{
    public const string EndpointTagName = "Flight";
    public const string Prefix = "/flights";

    public class ServiceManager : IServiceDiscovery
    {
        public void AddServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<FlightService>();
        }
    }
}