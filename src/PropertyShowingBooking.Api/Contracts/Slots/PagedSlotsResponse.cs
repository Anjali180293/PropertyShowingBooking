namespace PropertyShowingBooking.Api.Contracts.Slots;

public sealed record PagedSlotsResponse(
    IReadOnlyList<SlotResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
