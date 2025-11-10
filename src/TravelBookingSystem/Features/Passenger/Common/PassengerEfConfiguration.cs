using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelBookingSystem.Common.Persistence;

namespace TravelBookingSystem.Features.Passenger.Common;

public class PassengerEfConfiguration : IEntityTypeConfiguration<Passenger>
{
    public void Configure(EntityTypeBuilder<Passenger> builder)
    {
        builder.ToTable(TravelBookingDbContextSchema.Passenger.TableName);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.HasIndex(x => x.Email)
            .IsUnique();
        
        builder.HasIndex(x => x.PassportNumber)
            .IsUnique();

        builder.Property(x => x.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.FullName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.PassportNumber)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(32)
            .IsRequired(false);

    }
}