using Microsoft.EntityFrameworkCore;
using PropertyShowingBooking.Api.Contracts.Slots;
using PropertyShowingBooking.Api.Data;
using PropertyShowingBooking.Domain.Entities;

namespace PropertyShowingBooking.Api.Services;

public class SlotService(BookingDbContext db) : ISlotService
{
    private const int MinDurationMinutes = 15;
    private const int MaxDurationMinutes = 120;
    private const int MinCapacity = 1;
    private const int MaxCapacity = 20;
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<ServiceResult<SlotResponse>> CreateAsync(
        CreateSlotRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = ValidateCreateRequest(request);
        if (errors.Count > 0)
        {
            return ServiceResult<SlotResponse>.Failure(errors);
        }

        var slot = new Slot
        {
            Id = Guid.NewGuid(),
            PropertyId = request.PropertyId,
            StartsAt = request.StartsAt,
            DurationMinutes = request.DurationMinutes,
            Capacity = request.Capacity,
            SeatsBooked = 0
        };

        db.Slots.Add(slot);
        await db.SaveChangesAsync(cancellationToken);

        return ServiceResult<SlotResponse>.Success(ToResponse(slot));
    }

    public async Task<ServiceResult<PagedSlotsResponse>> GetAsync(
        GetSlotsQuery query,
        CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, string[]>();

        if (query.From.HasValue && query.To.HasValue && query.From > query.To)
        {
            errors["from"] = ["'from' must be less than or equal to 'to'."];
        }

        var currentPage = query.Page ?? DefaultPage;
        var currentPageSize = query.PageSize ?? DefaultPageSize;

        if (currentPage < 1)
        {
            errors["page"] = ["page must be greater than or equal to 1."];
        }

        if (currentPageSize < 1 || currentPageSize > MaxPageSize)
        {
            errors["pageSize"] = [$"pageSize must be between 1 and {MaxPageSize}."];
        }

        if (errors.Count > 0)
        {
            return ServiceResult<PagedSlotsResponse>.Failure(errors);
        }

        var slotsQuery = db.Slots.AsNoTracking();

        if (query.PropertyId.HasValue)
        {
            slotsQuery = slotsQuery.Where(s => s.PropertyId == query.PropertyId.Value);
        }

        if (query.From.HasValue)
        {
            slotsQuery = slotsQuery.Where(s => s.StartsAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            slotsQuery = slotsQuery.Where(s => s.StartsAt <= query.To.Value);
        }

        var totalCount = await slotsQuery.CountAsync(cancellationToken);

        var items = await slotsQuery
            .OrderBy(s => s.StartsAt)
            .Skip((currentPage - 1) * currentPageSize)
            .Take(currentPageSize)
            .Select(s => new SlotResponse(
                s.Id,
                s.PropertyId,
                s.StartsAt,
                s.DurationMinutes,
                s.Capacity,
                s.SeatsBooked,
                s.Capacity - s.SeatsBooked))
            .ToListAsync(cancellationToken);

        return ServiceResult<PagedSlotsResponse>.Success(
            new PagedSlotsResponse(items, currentPage, currentPageSize, totalCount));
    }

    private static Dictionary<string, string[]> ValidateCreateRequest(CreateSlotRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.PropertyId == Guid.Empty)
        {
            errors["propertyId"] = ["propertyId is required."];
        }

        if (request.StartsAt <= DateTimeOffset.UtcNow)
        {
            errors["startsAt"] = ["startsAt must be in the future."];
        }

        if (request.DurationMinutes < MinDurationMinutes || request.DurationMinutes > MaxDurationMinutes)
        {
            errors["durationMinutes"] =
                [$"durationMinutes must be between {MinDurationMinutes} and {MaxDurationMinutes}."];
        }

        if (request.Capacity < MinCapacity || request.Capacity > MaxCapacity)
        {
            errors["capacity"] = [$"capacity must be between {MinCapacity} and {MaxCapacity}."];
        }

        return errors;
    }

    private static SlotResponse ToResponse(Slot slot) =>
        new(
            slot.Id,
            slot.PropertyId,
            slot.StartsAt,
            slot.DurationMinutes,
            slot.Capacity,
            slot.SeatsBooked,
            slot.SeatsRemaining);
}
