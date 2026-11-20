
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PropertyShowingBooking.Api.Contracts.Bookings;
using PropertyShowingBooking.Api.Data;
using PropertyShowingBooking.Api.Services;
using PropertyShowingBooking.Domain.Entities;
using PropertyShowingBooking.Domain.Enums;

namespace PropertyShowingBooking.Tests;

public class UnitTest1
{
    [Fact]
    public async Task BookSlotAsync_WithValidRequest_CreatesBookingAndReservesSeats()
    {
        // Arrange: Create an in-memory SQLite database.
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new BookingDbContext(options);

        await db.Database.EnsureCreatedAsync();

        // Create a slot with capacity of 10 seats.
        var slot = new Slot
        {
            Id = Guid.NewGuid(),
            PropertyId = Guid.NewGuid(),
            StartsAt = DateTimeOffset.UtcNow.AddDays(2),
            DurationMinutes = 30,
            Capacity = 10,
            SeatsBooked = 0
        };

        db.Slots.Add(slot);
        await db.SaveChangesAsync();

        var service = new BookingService(db);

        var request = new CreateBookingRequest(
            "test@example.com",
            2);

        // Act: Book 2 seats.
        var result = await service.BookSlotAsync(slot.Id, request);

        // Assert: Booking should be successful.
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        Assert.Equal(BookingStatus.Confirmed, result.Value.Status);
        Assert.Equal(2, result.Value.PartySize);
        Assert.Equal("test@example.com", result.Value.RenterEmail);

        // Verify that the slot's booked seats increased.
        var updatedSlot = await db.Slots
    .AsNoTracking()
    .SingleAsync(s => s.Id == slot.Id);

        Assert.Equal(2, updatedSlot.SeatsBooked);
        Assert.Equal(8, updatedSlot.SeatsRemaining);

        // Verify that the booking was saved.
        var savedBooking = await db.Bookings
            .SingleAsync(b => b.SlotId == slot.Id);

        Assert.Equal(BookingStatus.Confirmed, savedBooking.Status);

        // Verify that the initial history record was created.
        var history = await db.BookingHistories
            .SingleAsync(h => h.BookingId == savedBooking.Id);

        Assert.Null(history.FromStatus);
        Assert.Equal(BookingStatus.Confirmed, history.ToStatus);
        Assert.Equal(BookingTransitionRules.Renter, history.Actor);
    }


[Fact]
public async Task BookSlotAsync_WhenCapacityIsInsufficient_ReturnsConflict()
{
    // Arrange
    await using var connection =
        new SqliteConnection("Data Source=:memory:");

    await connection.OpenAsync();

    var options = new DbContextOptionsBuilder<BookingDbContext>()
        .UseSqlite(connection)
        .Options;

    await using var db = new BookingDbContext(options);

    await db.Database.EnsureCreatedAsync();

    var slot = new Slot
    {
        Id = Guid.NewGuid(),
        PropertyId = Guid.NewGuid(),
        StartsAt = DateTimeOffset.UtcNow.AddDays(2),
        DurationMinutes = 30,
        Capacity = 2,
        SeatsBooked = 0
    };

    db.Slots.Add(slot);
    await db.SaveChangesAsync();

    var service = new BookingService(db);

    // Request 3 seats when only 2 are available.
    var request = new CreateBookingRequest(
        "test@example.com",
        3);

    // Act
    var result = await service.BookSlotAsync(slot.Id, request);

    // Assert
    Assert.False(result.IsSuccess);
    Assert.Equal(ServiceFailureKind.Conflict, result.FailureKind);

    // No seats should have been reserved.
    var updatedSlot = await db.Slots
    .AsNoTracking()
    .SingleAsync(s => s.Id == slot.Id);

    Assert.Equal(0, updatedSlot.SeatsBooked);

    // No booking should have been created.
    Assert.Empty(await db.Bookings.ToListAsync());

    // No history should have been created.
    Assert.Empty(await db.BookingHistories.ToListAsync());
}

[Fact]
public async Task BookSlotAsync_WhenRenterAlreadyHasActiveBooking_ReturnsConflict()
{
    // Arrange
    await using var connection =
        new SqliteConnection("Data Source=:memory:");

    await connection.OpenAsync();

    var options = new DbContextOptionsBuilder<BookingDbContext>()
        .UseSqlite(connection)
        .Options;

    await using var db = new BookingDbContext(options);

    await db.Database.EnsureCreatedAsync();

    var slot = new Slot
    {
        Id = Guid.NewGuid(),
        PropertyId = Guid.NewGuid(),
        StartsAt = DateTimeOffset.UtcNow.AddDays(2),
        DurationMinutes = 30,
        Capacity = 10,
        SeatsBooked = 0
    };

    db.Slots.Add(slot);
    await db.SaveChangesAsync();

    var service = new BookingService(db);

    // First booking
    var firstRequest = new CreateBookingRequest(
        "test@example.com",
        2);

    var firstResult = await service.BookSlotAsync(
        slot.Id,
        firstRequest);

    Assert.True(firstResult.IsSuccess);

    // Second booking using the same email with different casing
    var secondRequest = new CreateBookingRequest(
        "TEST@example.com",
        2);

    // Act
    var secondResult = await service.BookSlotAsync(
        slot.Id,
        secondRequest);

    // Assert
    Assert.False(secondResult.IsSuccess);
    Assert.Equal(
        ServiceFailureKind.Conflict,
        secondResult.FailureKind);

    // Only one booking should exist.
    Assert.Equal(
        1,
        await db.Bookings.CountAsync());

    // Only the first booking's seats should be reserved.
    var updatedSlot = await db.Slots
    .AsNoTracking()
    .SingleAsync(s => s.Id == slot.Id);

    Assert.Equal(2, updatedSlot.SeatsBooked);

    // Only the initial booking history should exist.
    Assert.Equal(
        1,
        await db.BookingHistories.CountAsync());
}


[Fact]
public async Task TransitionAsync_WhenBookingIsCancelled_ReleasesReservedSeats()
{
    // Arrange
    await using var connection =
        new SqliteConnection("Data Source=:memory:");

    await connection.OpenAsync();

    var options = new DbContextOptionsBuilder<BookingDbContext>()
        .UseSqlite(connection)
        .Options;

    await using var db = new BookingDbContext(options);
    await db.Database.EnsureCreatedAsync();

    var slot = new Slot
    {
        Id = Guid.NewGuid(),
        PropertyId = Guid.NewGuid(),
        StartsAt = DateTimeOffset.UtcNow.AddDays(2),
        DurationMinutes = 30,
        Capacity = 10,
        SeatsBooked = 0
    };

    db.Slots.Add(slot);
    await db.SaveChangesAsync();

    var service = new BookingService(db);

    var bookingResult = await service.BookSlotAsync(
        slot.Id,
        new CreateBookingRequest("test@example.com", 3));

    Assert.True(bookingResult.IsSuccess);
    Assert.NotNull(bookingResult.Value);

    var bookingId = bookingResult.Value.Id;

    // Act: Renter cancels the booking.
    var transitionResult = await service.TransitionAsync(
        bookingId,
        new TransitionBookingRequest(
            BookingStatus.Cancelled,
            "Plans changed",
            "renter"));

    // Assert
    Console.WriteLine($"Success: {transitionResult.IsSuccess}");
Console.WriteLine($"Failure kind: {transitionResult.FailureKind}");
Console.WriteLine($"Error message: {transitionResult.ErrorMessage}");
    Assert.True(transitionResult.IsSuccess);
    Assert.Equal(
        BookingStatus.Cancelled,
        transitionResult.Value!.Status);

    var updatedSlot = await db.Slots
    .AsNoTracking()
    .SingleAsync(s => s.Id == slot.Id);

    Assert.Equal(0, updatedSlot.SeatsBooked);
    Assert.Equal(10, updatedSlot.SeatsRemaining);

    var history = await db.BookingHistories
    .Where(h => h.BookingId == bookingId)
    .ToListAsync();

history = history
    .OrderBy(h => h.ChangedAt)
    .ToList();

    Assert.Equal(2, history.Count);
    Assert.Equal(BookingStatus.Confirmed, history[1].FromStatus);
    Assert.Equal(BookingStatus.Cancelled, history[1].ToStatus);
    Assert.Equal("renter", history[1].Actor);
    Assert.Equal("Plans changed", history[1].Reason);
}

[Fact]
public async Task TransitionAsync_WhenRenterMarksBookingAsAttended_ReturnsConflict()
{
    await using var connection = new SqliteConnection("Data Source=:memory:");
    await connection.OpenAsync();

    var options = new DbContextOptionsBuilder<BookingDbContext>()
        .UseSqlite(connection)
        .Options;

    await using var db = new BookingDbContext(options);
    await db.Database.EnsureCreatedAsync();

    var slot = new Slot
    {
        Id = Guid.NewGuid(),
        PropertyId = Guid.NewGuid(),
        StartsAt = DateTimeOffset.UtcNow.AddMinutes(-10),
        DurationMinutes = 30,
        Capacity = 10,
        SeatsBooked = 0
    };

    db.Slots.Add(slot);
    await db.SaveChangesAsync();

    var service = new BookingService(db);

    var bookingResult = await service.BookSlotAsync(
        slot.Id,
        new CreateBookingRequest("test@example.com", 2));

    Assert.True(bookingResult.IsSuccess);
    var bookingId = bookingResult.Value!.Id;

    var result = await service.TransitionAsync(
        bookingId,
        new TransitionBookingRequest(
            BookingStatus.Attended,
            "Showing completed",
            "renter"));

    Assert.False(result.IsSuccess);
    Assert.Equal(ServiceFailureKind.Conflict, result.FailureKind);

    var booking = await db.Bookings
        .AsNoTracking()
        .SingleAsync(b => b.Id == bookingId);

    Assert.Equal(BookingStatus.Confirmed, booking.Status);
}

[Fact]
public async Task TransitionAsync_WhenOwnerMarksBookingAsAttendedBeforeStart_ReturnsConflict()
{
    await using var connection = new SqliteConnection("Data Source=:memory:");
    await connection.OpenAsync();

    var options = new DbContextOptionsBuilder<BookingDbContext>()
        .UseSqlite(connection)
        .Options;

    await using var db = new BookingDbContext(options);
    await db.Database.EnsureCreatedAsync();

    var slot = new Slot
    {
        Id = Guid.NewGuid(),
        PropertyId = Guid.NewGuid(),
        StartsAt = DateTimeOffset.UtcNow.AddDays(2),
        DurationMinutes = 30,
        Capacity = 10,
        SeatsBooked = 0
    };

    db.Slots.Add(slot);
    await db.SaveChangesAsync();

    var service = new BookingService(db);

    var bookingResult = await service.BookSlotAsync(
        slot.Id,
        new CreateBookingRequest("test@example.com", 2));

    Assert.True(bookingResult.IsSuccess);
    var bookingId = bookingResult.Value!.Id;

    var result = await service.TransitionAsync(
        bookingId,
        new TransitionBookingRequest(
            BookingStatus.Attended,
            "Showing completed",
            "owner"));

    Assert.False(result.IsSuccess);
    Assert.Equal(ServiceFailureKind.Conflict, result.FailureKind);

    var booking = await db.Bookings
        .AsNoTracking()
        .SingleAsync(b => b.Id == bookingId);

    Assert.Equal(BookingStatus.Confirmed, booking.Status);
}

[Fact]
public async Task BookSlotAsync_WhenTwoRentersBookLastSeatConcurrently_OnlyOneSucceeds()
{
    var connectionString =
        "Data Source=ConcurrencyTestDb;Mode=Memory;Cache=Shared";

    await using var anchorConnection =
        new SqliteConnection(connectionString);

    await anchorConnection.OpenAsync();

    var options = new DbContextOptionsBuilder<BookingDbContext>()
        .UseSqlite(connectionString)
        .Options;

    // Create the database and slot.
    await using (var setupDb = new BookingDbContext(options))
    {
        await setupDb.Database.EnsureCreatedAsync();

        var slot = new Slot
        {
            Id = Guid.NewGuid(),
            PropertyId = Guid.NewGuid(),
            StartsAt = DateTimeOffset.UtcNow.AddDays(2),
            DurationMinutes = 30,
            Capacity = 1,
            SeatsBooked = 0
        };

        setupDb.Slots.Add(slot);
        await setupDb.SaveChangesAsync();

        var slotId = slot.Id;

        // Use separate DbContext instances to simulate
        // two independent booking requests.
        var bookingTask1 = Task.Run(async () =>
        {
            await using var db1 = new BookingDbContext(options);
            var service1 = new BookingService(db1);

            return await service1.BookSlotAsync(
                slotId,
                new CreateBookingRequest("renter1@example.com", 1));
        });

        var bookingTask2 = Task.Run(async () =>
        {
            await using var db2 = new BookingDbContext(options);
            var service2 = new BookingService(db2);

            return await service2.BookSlotAsync(
                slotId,
                new CreateBookingRequest("renter2@example.com", 1));
        });

        var results = await Task.WhenAll(bookingTask1, bookingTask2);

        // Exactly one booking should succeed.
        Assert.Single(results.Where(r => r.IsSuccess));

        // The other booking should fail with a conflict.
        Assert.Single(results.Where(r =>
            !r.IsSuccess &&
            r.FailureKind == ServiceFailureKind.Conflict));

        // Verify the final database state.
        await using var verificationDb =
            new BookingDbContext(options);

        var updatedSlot = await verificationDb.Slots
            .AsNoTracking()
            .SingleAsync(s => s.Id == slotId);

        Assert.Equal(1, updatedSlot.SeatsBooked);
        Assert.Equal(0, updatedSlot.SeatsRemaining);

        var bookings = await verificationDb.Bookings
            .AsNoTracking()
            .Where(b => b.SlotId == slotId)
            .ToListAsync();

        Assert.Single(bookings);
        Assert.Equal(BookingStatus.Confirmed, bookings[0].Status);
    }
}

[Fact]
public async Task TransitionAsync_WhenOwnerMarksBookingAsAttendedAfterStart_Succeeds()
{
    await using var connection = new SqliteConnection("Data Source=:memory:");
    await connection.OpenAsync();

    var options = new DbContextOptionsBuilder<BookingDbContext>()
        .UseSqlite(connection)
        .Options;

    await using var db = new BookingDbContext(options);
    await db.Database.EnsureCreatedAsync();

    var slot = new Slot
    {
        Id = Guid.NewGuid(),
        PropertyId = Guid.NewGuid(),
        StartsAt = DateTimeOffset.UtcNow.AddMinutes(-10),
        DurationMinutes = 30,
        Capacity = 10,
        SeatsBooked = 0
    };

    db.Slots.Add(slot);
    await db.SaveChangesAsync();

    var service = new BookingService(db);

    var bookingResult = await service.BookSlotAsync(
        slot.Id,
        new CreateBookingRequest("test@example.com", 2));

    Assert.True(bookingResult.IsSuccess);
    var bookingId = bookingResult.Value!.Id;

    var result = await service.TransitionAsync(
        bookingId,
        new TransitionBookingRequest(
            BookingStatus.Attended,
            "Showing completed",
            "owner"));

    Assert.True(result.IsSuccess);
    Assert.Equal(BookingStatus.Attended, result.Value!.Status);

    var booking = await db.Bookings
        .AsNoTracking()
        .SingleAsync(b => b.Id == bookingId);

    Assert.Equal(BookingStatus.Attended, booking.Status);

    var history = await db.BookingHistories
        .Where(h => h.BookingId == bookingId)
        .ToListAsync();

    Assert.Equal(2, history.Count);
    Assert.Contains(history, h =>
        h.FromStatus == BookingStatus.Confirmed &&
        h.ToStatus == BookingStatus.Attended &&
        h.Actor == "owner");
}

}
