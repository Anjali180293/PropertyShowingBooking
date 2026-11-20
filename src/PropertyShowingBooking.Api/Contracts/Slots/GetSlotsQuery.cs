namespace PropertyShowingBooking.Api.Contracts.Slots;

public sealed record GetSlotsQuery(
    Guid? PropertyId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int? Page,
    int? PageSize);
