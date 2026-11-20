using PropertyShowingBooking.Domain.Enums;

namespace PropertyShowingBooking.Api.Contracts.Bookings;

public sealed record BookingResponse(
    Guid Id,
    Guid SlotId,
    string RenterEmail,
    int PartySize,
    BookingStatus Status,
    DateTimeOffset CreatedAt);
