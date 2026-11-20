using PropertyShowingBooking.Domain.Enums;

namespace PropertyShowingBooking.Api.Contracts.Bookings;

public sealed record BookingHistoryResponse(
    Guid Id,
    BookingStatus? FromStatus,
    BookingStatus ToStatus,
    string Actor,
    string? Reason,
    DateTimeOffset ChangedAt);
