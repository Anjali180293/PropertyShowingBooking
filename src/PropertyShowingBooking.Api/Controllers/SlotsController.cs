using Microsoft.AspNetCore.Mvc;
using PropertyShowingBooking.Api.Contracts.Bookings;
using PropertyShowingBooking.Api.Contracts.Slots;
using PropertyShowingBooking.Api.Services;

namespace PropertyShowingBooking.Api.Controllers;

[ApiController]
[Route("slots")]
public class SlotsController(ISlotService slotService, IBookingService bookingService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(SlotResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSlot(
        [FromBody] CreateSlotRequest request,
        CancellationToken cancellationToken)
    {
        var result = await slotService.CreateAsync(request, cancellationToken);
        return this.FromServiceResult(
            result,
            value => CreatedAtAction(nameof(GetSlots), new { propertyId = value.PropertyId }, value));
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedSlotsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSlots(
        [FromQuery] Guid? propertyId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var result = await slotService.GetAsync(
            new GetSlotsQuery(propertyId, from, to, page, pageSize),
            cancellationToken);

        return this.FromServiceResult(result, Ok);
    }

    [HttpPost("{id:guid}/bookings")]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BookSlot(
        Guid id,
        [FromBody] CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await bookingService.BookSlotAsync(id, request, cancellationToken);
        return this.FromServiceResult(
            result,
            value => Created($"/bookings/{value.Id}", value));
    }
}

