using PropertyShowingBooking.Api.Contracts.Slots;

namespace PropertyShowingBooking.Api.Services;

public interface ISlotService
{
    Task<ServiceResult<SlotResponse>> CreateAsync(
        CreateSlotRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedSlotsResponse>> GetAsync(
        GetSlotsQuery query,
        CancellationToken cancellationToken = default);
}
