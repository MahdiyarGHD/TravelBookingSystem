using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelBookingSystem.Common.Persistence;

namespace TravelBookingSystem.Features.Booking.Common;

public class BookingEfConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable(TravelBookingDbContextSchema.Booking.TableName);

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.PassengerId);
        builder.HasIndex(x => x.FlightId);
        
        builder.Property(x => x.Id)
            .ValueGeneratedNever();
        
        builder.Property(x => x.BookingDate)
            .IsRequired();

        builder.Property(x => x.SeatNumber)
            .IsRequired();
        
        builder.HasOne(b => b.Passenger)
            .WithMany(p => p.Bookings)
            .HasForeignKey(b => b.PassengerId);

        builder.HasOne(b => b.Flight)
            .WithMany(f => f.Bookings)
            .HasForeignKey(b => b.FlightId);
        
        builder.HasIndex(b => new { b.FlightId, b.SeatNumber })
            .IsUnique();
    }
}