using PropertyShowingBooking.Api.Contracts.Bookings;

namespace PropertyShowingBooking.Api.Services;

public interface IBookingService
{
    Task<ServiceResult<BookingResponse>> BookSlotAsync(
        Guid slotId,
        CreateBookingRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<BookingResponse>> TransitionAsync(
        Guid bookingId,
        TransitionBookingRequest request,
        CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<BookingHistoryResponse>>> GetHistoryAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default);
}
