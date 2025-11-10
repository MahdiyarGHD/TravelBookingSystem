using ServiceCollector.Abstractions;
using TravelBookingSystem.Features.Flight.Common;

namespace TravelBookingSystem.Features.Booking;

public abstract class FeatureManager
{
    public const string EndpointTagName = "Booking";
    public const string Prefix = "/bookings";

    public class ServiceManager : IServiceDiscovery
    {
        public void AddServices(IServiceCollection serviceCollection)
        {
            // serviceCollection.AddScoped<BookingService>();
        }
    }
}