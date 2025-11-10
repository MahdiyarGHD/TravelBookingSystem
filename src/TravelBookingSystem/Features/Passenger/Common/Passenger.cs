namespace TravelBookingSystem.Features.Passenger.Common;

public class Passenger
{
    public Guid Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PassportNumber { get; set; }
    public string? PhoneNumber { get; set; }
    
    public static Passenger Create(
        string fullName,
        string email,
        string passportNumber,
        string? phoneNumber)
    {
        return new Passenger
        {
            Id = Guid.CreateVersion7(), 
            FullName = fullName,
            Email = email,
            PassportNumber = passportNumber,
            PhoneNumber = phoneNumber
        };
    }
}