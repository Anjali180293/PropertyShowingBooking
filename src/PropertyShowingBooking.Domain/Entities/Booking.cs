using PropertyShowingBooking.Domain.Enums;

namespace PropertyShowingBooking.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SlotId { get; set; }
    public string RenterEmail { get; set; } = string.Empty;
    public int PartySize { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;
    public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;

    public Slot Slot { get; set; } = null!;
    public ICollection<BookingHistory> History { get; set; } = new List<BookingHistory>();
}
