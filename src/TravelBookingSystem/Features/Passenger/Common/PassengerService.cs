using EasyMicroservices.ServiceContracts;
using Microsoft.EntityFrameworkCore;
using TravelBookingSystem.Common.Persistence;

namespace TravelBookingSystem.Features.Passenger.Common;

public class PassengerService(
    TravelBookingDbContext dbContext,
    TravelBookingDbContextReadOnly readOnlyDbContext
    )
{
    private readonly TravelBookingDbContext _dbContext = dbContext;
    private readonly TravelBookingDbContextReadOnly _readOnlyDbContext = readOnlyDbContext;
    public async Task<MessageContract<Guid>> CreateAsync(
        string fullName,
        string email,
        string passportNumber,
        string? phoneNumber,
        CancellationToken cancellationToken = default)
    {
        var exists = await _readOnlyDbContext.Passengers
            .AnyAsync(p => phoneNumber != null && p.PhoneNumber == phoneNumber, cancellationToken: cancellationToken);

        if (exists)
            return (FailedReasonType.Incorrect, "Passenger with same phone number already exists.");
        
        var passenger = Passenger.Create(
            fullName,
            email,
            passportNumber,
            phoneNumber
        );

        _dbContext.Passengers.Add(passenger);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate") is true)
        {
            return (FailedReasonType.Incorrect, "Duplicate passenger detected.");
        }
        
        return passenger.Id;
    }

    public async Task<List<Passenger>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _readOnlyDbContext.Passengers
            .ToListAsync(cancellationToken: cancellationToken);
}