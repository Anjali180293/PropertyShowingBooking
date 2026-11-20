namespace PropertyShowingBooking.Api.Contracts.Slots;

public sealed record SlotResponse(
    Guid Id,
    Guid PropertyId,
    DateTimeOffset StartsAt,
    int DurationMinutes,
    int Capacity,
    int SeatsBooked,
    int SeatsRemaining);
