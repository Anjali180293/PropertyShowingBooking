using PropertyShowingBooking.Domain.Enums;

namespace PropertyShowingBooking.Domain.Entities;

public class BookingHistory
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public BookingStatus? FromStatus { get; set; }
    public BookingStatus ToStatus { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTimeOffset ChangedAt { get; set; }

    public Booking Booking { get; set; } = null!;
}
