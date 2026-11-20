namespace PropertyShowingBooking.Domain.Entities;

public class Slot
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public int DurationMinutes { get; set; }
    public int Capacity { get; set; }
    public int SeatsBooked { get; set; }
    public int SeatsRemaining => Capacity - SeatsBooked;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
