namespace PropertyShowingBooking.Api.Contracts.Slots;

public sealed record CreateSlotRequest(
    Guid PropertyId,
    DateTimeOffset StartsAt,
    int DurationMinutes,
    int Capacity);
