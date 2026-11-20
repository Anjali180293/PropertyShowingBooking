using PropertyShowingBooking.Domain.Enums;

namespace PropertyShowingBooking.Api.Contracts.Bookings;

public sealed record TransitionBookingRequest(
    BookingStatus To,
    string? Reason,
    string Actor);
