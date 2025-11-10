using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelBookingSystem.Common.Persistence;

namespace TravelBookingSystem.Features.Flight.Common;

public class FlightEfConfiguration : IEntityTypeConfiguration<Flight>
{
    
    
    public void Configure(EntityTypeBuilder<Flight> builder)
    {
        builder.ToTable(TravelBookingDbContextSchema.Flight.TableName);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.HasIndex(x => x.FlightNumber)
            .IsUnique();

        builder.Property(x => x.FlightNumber)
            .IsRequired();
        
        builder.Property(x => x.Destination)
            .IsRequired()
            .HasMaxLength(30);
        
        builder.Property(x => x.Origin)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.ArrivalTime)
            .IsRequired();

        builder.Property(x => x.DepartureTime)
            .IsRequired();

        builder.Property(x => x.AvailableSeats)
            .IsRequired();

        builder.Property(x => x.Price)
            .IsRequired();
    }
}