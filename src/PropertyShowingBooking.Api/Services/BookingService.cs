using System.Net.Mail;
using PropertyShowingBooking.Api.Contracts.Bookings;
using PropertyShowingBooking.Api.Data;
using PropertyShowingBooking.Domain.Entities;
using PropertyShowingBooking.Domain.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace PropertyShowingBooking.Api.Services;

public class BookingService(BookingDbContext db) : IBookingService
{
    private const int MinPartySize = 1;
    private const int MaxPartySize = 6;

    
public async Task<ServiceResult<BookingResponse>> BookSlotAsync(
    Guid slotId,
    CreateBookingRequest request,
    CancellationToken cancellationToken = default)
{
    var errors = ValidateCreateRequest(request);
    if (errors.Count > 0)
    {
        return ServiceResult<BookingResponse>.Failure(errors);
    }

    try
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        // Normalize email so different casing is treated as the same renter.
        var normalizedEmail = request.RenterEmail.Trim().ToLowerInvariant();

        // Check whether this renter already has an active booking.
        var existingBooking = await db.Bookings.AnyAsync(
            b => b.SlotId == slotId &&
                 b.RenterEmail == normalizedEmail &&
                 b.Status == BookingStatus.Confirmed,
            cancellationToken);

        if (existingBooking)
        {
            return ServiceResult<BookingResponse>.Conflict(
                "Renter already has an active booking for this slot.");
        }

        // Atomically reserve seats only if enough capacity is available.
        var affectedRows = await db.Slots
            .Where(s =>
                s.Id == slotId &&
                s.SeatsBooked + request.PartySize <= s.Capacity)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    s => s.SeatsBooked,
                    s => s.SeatsBooked + request.PartySize),
                cancellationToken);

        // If no row was updated, the slot is missing or has insufficient seats.
        if (affectedRows == 0)
        {
            var slotExists = await db.Slots.AnyAsync(
                s => s.Id == slotId,
                cancellationToken);

            if (!slotExists)
            {
                return ServiceResult<BookingResponse>.NotFound(
                    $"Slot '{slotId}' was not found.");
            }

            return ServiceResult<BookingResponse>.Conflict(
                "Not enough seats remaining.");
        }

        var now = DateTimeOffset.UtcNow;

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            SlotId = slotId,
            RenterEmail = normalizedEmail,
            PartySize = request.PartySize,
            Status = BookingStatus.Confirmed,
            CreatedAt = now
        };

        db.Bookings.Add(booking);

        db.BookingHistories.Add(new BookingHistory
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            FromStatus = null,
            ToStatus = BookingStatus.Confirmed,
            Actor = BookingTransitionRules.Renter,
            Reason = "Booking created",
            ChangedAt = now
        });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ServiceResult<BookingResponse>.Success(ToResponse(booking));
    }
    catch (DbUpdateException ex) when (
        ex.InnerException is SqliteException sqliteEx &&
        sqliteEx.SqliteErrorCode == 19)
    {
        return ServiceResult<BookingResponse>.Conflict(
            "Renter already has an active booking for this slot.");
    }
    catch (SqliteException ex) when (
        ex.SqliteErrorCode == 5 || ex.SqliteErrorCode == 6)
    {
        return ServiceResult<BookingResponse>.Conflict(
            "The slot is currently being booked by another request. Please retry.");
    }
}


   
public async Task<ServiceResult<BookingResponse>> TransitionAsync(
    Guid bookingId,
    TransitionBookingRequest request,
    CancellationToken cancellationToken = default)
{
    var errors = new Dictionary<string, string[]>();

    if (!BookingTransitionRules.IsValidActor(request.Actor))
    {
        errors["actor"] = ["actor must be 'owner' or 'renter'."];
    }

    if (!Enum.IsDefined(request.To))
    {
        errors["to"] = ["to must be a valid booking status."];
    }

    if (errors.Count > 0)
    {
        return ServiceResult<BookingResponse>.Failure(errors);
    }

    try
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        var booking = await db.Bookings
            .Include(b => b.Slot)
            .FirstOrDefaultAsync(
                b => b.Id == bookingId,
                cancellationToken);

        if (booking is null)
        {
            return ServiceResult<BookingResponse>.NotFound(
                $"Booking '{bookingId}' was not found.");
        }

        var actor = BookingTransitionRules.NormalizeActor(request.Actor);
        var now = DateTimeOffset.UtcNow;

        if (!BookingTransitionRules.CanTransition(
            booking.Status,
            request.To,
            actor,
            booking.Slot.StartsAt,
            now))
        {
            return ServiceResult<BookingResponse>.Conflict(
                $"Transition from '{booking.Status}' to '{request.To}' by '{actor}' is not allowed at this time.");
        }

        // Change the status only if it is still Confirmed in the database.
        // This prevents two simultaneous requests from transitioning the same booking.
        var affectedBookings = await db.Bookings
            .Where(b =>
                b.Id == bookingId &&
                b.Status == BookingStatus.Confirmed)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    b => b.Status,
                    request.To),
                cancellationToken);

        if (affectedBookings == 0)
        {
            return ServiceResult<BookingResponse>.Conflict(
                "This booking has already been transitioned.");
        }

        var fromStatus = booking.Status;

        // Release the seats when a confirmed booking is cancelled.
        if (request.To == BookingStatus.Cancelled)
        {
            var affectedSlots = await db.Slots
                .Where(s =>
                    s.Id == booking.SlotId &&
                    s.SeatsBooked >= booking.PartySize)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        s => s.SeatsBooked,
                        s => s.SeatsBooked - booking.PartySize),
                    cancellationToken);

            if (affectedSlots == 0)
            {
                return ServiceResult<BookingResponse>.Conflict(
                    "Could not release seats because the slot's booked-seat count is inconsistent.");
            }
        }

        db.BookingHistories.Add(new BookingHistory
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            FromStatus = fromStatus,
            ToStatus = request.To,
            Actor = actor,
            Reason = string.IsNullOrWhiteSpace(request.Reason)
                ? null
                : request.Reason.Trim(),
            ChangedAt = now
        });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        // ExecuteUpdateAsync bypasses EF's change tracker.
        booking.Status = request.To;

        return ServiceResult<BookingResponse>.Success(ToResponse(booking));
    }
    catch (SqliteException ex) when (
        ex.SqliteErrorCode == 5 || ex.SqliteErrorCode == 6)
    {
        return ServiceResult<BookingResponse>.Conflict(
            "The booking is being updated by another request. Please retry.");
    }
}


    public async Task<ServiceResult<IReadOnlyList<BookingHistoryResponse>>> GetHistoryAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        var exists = await db.Bookings.AnyAsync(b => b.Id == bookingId, cancellationToken);
        if (!exists)
        {
            return ServiceResult<IReadOnlyList<BookingHistoryResponse>>.NotFound(
                $"Booking '{bookingId}' was not found.");
        }

        var history = await db.BookingHistories
            .AsNoTracking()
            .Where(h => h.BookingId == bookingId)
            .OrderBy(h => h.ChangedAt)
            .Select(h => new BookingHistoryResponse(
                h.Id,
                h.FromStatus,
                h.ToStatus,
                h.Actor,
                h.Reason,
                h.ChangedAt))
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyList<BookingHistoryResponse>>.Success(history);
    }

    private static Dictionary<string, string[]> ValidateCreateRequest(CreateBookingRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.RenterEmail) || !IsValidEmail(request.RenterEmail))
        {
            errors["renterEmail"] = ["renterEmail must be a valid email address."];
        }

        if (request.PartySize < MinPartySize || request.PartySize > MaxPartySize)
        {
            errors["partySize"] = [$"partySize must be between {MinPartySize} and {MaxPartySize}."];
        }

        return errors;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email.Trim());
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static BookingResponse ToResponse(Booking booking) =>
        new(
            booking.Id,
            booking.SlotId,
            booking.RenterEmail,
            booking.PartySize,
            booking.Status,
            booking.CreatedAt);
}
