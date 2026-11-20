using Microsoft.AspNetCore.Mvc;
using PropertyShowingBooking.Api.Contracts.Bookings;
using PropertyShowingBooking.Api.Services;

namespace PropertyShowingBooking.Api.Controllers;

[ApiController]
[Route("bookings")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    [HttpPost("{id:guid}/transitions")]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Transition(
        Guid id,
        [FromBody] TransitionBookingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await bookingService.TransitionAsync(id, request, cancellationToken);
        return this.FromServiceResult(result, Ok);
    }

    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<BookingHistoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistory(Guid id, CancellationToken cancellationToken)
    {
        var result = await bookingService.GetHistoryAsync(id, cancellationToken);
        return this.FromServiceResult(result, Ok);
    }
}
