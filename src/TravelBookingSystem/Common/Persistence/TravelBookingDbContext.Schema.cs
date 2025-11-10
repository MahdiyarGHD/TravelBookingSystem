namespace TravelBookingSystem.Common.Persistence;

public static class TravelBookingDbContextSchema
{
    public const string DefaultSchema = "TravelBooking";
    public const string DefaultConnectionStringName = "Main";
    
    public static class Flight
    {
        public const string TableName = "Flights";
    }
}