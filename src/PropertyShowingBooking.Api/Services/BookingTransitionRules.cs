
using PropertyShowingBooking.Domain.Enums;

namespace PropertyShowingBooking.Api.Services;

public static class BookingTransitionRules
{
    public const string Owner = "owner";
    public const string Renter = "renter";

    private static readonly HashSet<string> AllowedActors =
        new(StringComparer.OrdinalIgnoreCase) { Owner, Renter };

    private static readonly Dictionary<
        (BookingStatus From, BookingStatus To),
        HashSet<string>> Allowed = new()
    {
        [(BookingStatus.Confirmed, BookingStatus.Cancelled)] =
            new(StringComparer.OrdinalIgnoreCase) { Owner, Renter },

        [(BookingStatus.Confirmed, BookingStatus.Attended)] =
            new(StringComparer.OrdinalIgnoreCase) { Owner },

        [(BookingStatus.Confirmed, BookingStatus.NoShow)] =
            new(StringComparer.OrdinalIgnoreCase) { Owner }
    };

    public static bool IsValidActor(string actor) =>
        !string.IsNullOrWhiteSpace(actor) && AllowedActors.Contains(actor);

    public static string NormalizeActor(string actor) =>
        AllowedActors.First(a =>
            a.Equals(actor, StringComparison.OrdinalIgnoreCase));

    public static bool CanTransition(
        BookingStatus from,
        BookingStatus to,
        string actor,
        DateTimeOffset slotStartsAt,
        DateTimeOffset now)
    {
        if (!Allowed.TryGetValue((from, to), out var actors) ||
            !actors.Contains(actor))
        {
            return false;
        }

        if (to == BookingStatus.Cancelled)
        {
            return now < slotStartsAt;
        }

        if (to == BookingStatus.Attended ||
            to == BookingStatus.NoShow)
        {
            return now >= slotStartsAt;
        }

        return false;
    }
}
