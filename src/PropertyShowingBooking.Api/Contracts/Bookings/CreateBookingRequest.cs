namespace PropertyShowingBooking.Api.Contracts.Bookings;

public sealed record CreateBookingRequest(
    string RenterEmail,
    int PartySize);
