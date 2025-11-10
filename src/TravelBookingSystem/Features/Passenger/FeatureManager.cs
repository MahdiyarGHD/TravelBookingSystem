using ServiceCollector.Abstractions;
using TravelBookingSystem.Features.Flight.Common;
using TravelBookingSystem.Features.Passenger.Common;

namespace TravelBookingSystem.Features.Passenger;

public abstract class FeatureManager
{
    public const string EndpointTagName = "Passenger";
    public const string Prefix = "/passengers";

    public class ServiceManager : IServiceDiscovery
    {
        public void AddServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<PassengerService>();
        }
    }
}