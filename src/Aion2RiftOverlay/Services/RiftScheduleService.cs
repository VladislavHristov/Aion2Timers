using System;
using System.Collections.Generic;
using System.Linq;

namespace Aion2RiftOverlay.Services;

/// <summary>
/// Computes the next AION 2 "Space Rift" opening for Sofia, Bulgaria.
///
/// Schedule (user provided), defined in local Sofia wall-clock time:
///   - Daylight saving (EEST, UTC+3):  00:00 03:00 06:00 09:00 12:00 15:00 18:00 21:00
///   - Standard time   (EET,  UTC+2):  02:00 05:00 08:00 11:00 14:00 17:00 20:00 23:00
///   ("23:0" in the user's notes is interpreted as 23:00.)
///
/// Policies (documented and unit tested):
///   1. Which schedule applies on a given local date is decided by the DST state of
///      that date at 12:00 noon local time (12:00 never falls inside a DST transition).
///      This keeps the decision with the time-zone database rather than manual
///      month/offset math.
///   2. An opening whose local time does not exist because of a spring-forward
///      transition (e.g. 03:00 on the last Sunday of March) is skipped.
///   3. A local time repeated by a fall-back transition is interpreted at its second
///      occurrence (standard time), which is the default behaviour of
///      TimeZoneInfo.ConvertTimeToUtc for ambiguous times. (In practice no scheduled
///      time falls inside Bulgaria's repeated hour.)
///   4. An opening exactly at the current instant counts as the "current/active"
///      opening; <see cref="GetNextOpening"/> returns the next strictly later opening.
///   5. Remaining time is always recomputed from the current instant, never by
///      decrementing a stored counter.
/// </summary>
public sealed class RiftScheduleService
{
    public static readonly TimeSpan SlotInterval = TimeSpan.FromHours(3);

    private static readonly string[] WindowsTimeZoneIds = { "FLE Standard Time" };
    private static readonly string[] IanaTimeZoneIds = { "Europe/Sofia" };

    private static readonly int[] SummerHours = { 0, 3, 6, 9, 12, 15, 18, 21 };
    private static readonly int[] WinterHours = { 2, 5, 8, 11, 14, 17, 20, 23 };

    private readonly Func<DateTimeOffset> _clock;

    public RiftScheduleService(Func<DateTimeOffset>? clock = null)
    {
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        SofiaTimeZone = ResolveSofiaTimeZone();
    }

    /// <summary>
    /// Resolves the Sofia time zone. Prefers the Windows identifier
    /// "FLE Standard Time", then the IANA identifier "Europe/Sofia", and finally
    /// falls back to a fixed UTC+2 zone (no DST rules) so the app still works.
    /// </summary>
    public static TimeZoneInfo ResolveSofiaTimeZone()
    {
        foreach (var id in WindowsTimeZoneIds.Concat(IanaTimeZoneIds))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        // Last resort: fixed UTC+2 without DST rules (schedule slightly wrong
        // during summer, but the app remains functional).
        return TimeZoneInfo.CreateCustomTimeZone(
            "Aion2 Sofia Fallback", TimeSpan.FromHours(2), "Sofia (fallback)", "Sofia (fallback, no DST)");
    }
    /// <summary>
    /// All scheduled openings for one local Sofia date as concrete instants.
    /// Local times skipped by a DST transition are omitted.
    /// </summary>
    public IReadOnlyList<DateTimeOffset> GetOpeningsForLocalDate(DateTime localDate)
    {
        var hours = IsSummerSchedule(localDate) ? SummerHours : WinterHours;
        var openings = new List<DateTimeOffset>(hours.Length);

        foreach (var hour in hours)
        {
            var local = new DateTime(
                localDate.Year, localDate.Month, localDate.Day, hour, 0, 0, DateTimeKind.Unspecified);

            // Time skipped by a spring-forward transition: no opening happens.
            if (SofiaTimeZone.IsInvalidTime(local))
            {
                continue;
            }

            var utc = TimeZoneInfo.ConvertTimeToUtc(local, SofiaTimeZone);
            openings.Add(TimeZoneInfo.ConvertTime(new DateTimeOffset(utc), SofiaTimeZone));
        }

        return openings;
    }

    /// <summary>
    /// Scheduled openings within [now - 1 day, now + <paramref name="lookaheadDays"/> days),
    /// sorted ascending.
    /// </summary>
    public List<DateTimeOffset> GetUpcomingOpenings(DateTimeOffset now, int lookaheadDays = 4)
    {
        var localNow = TimeZoneInfo.ConvertTime(now, SofiaTimeZone);
        var result = new List<DateTimeOffset>();

        for (int dayOffset = -1; dayOffset <= lookaheadDays; dayOffset++)
        {
            result.AddRange(GetOpeningsForLocalDate(localNow.Date.AddDays(dayOffset)));
        }

        result.Sort();
        return result;
    }

    /// <summary>
    /// Next scheduled opening strictly after <paramref name="now"/> and the remaining
    /// time. Computed from the current instant each call, so it is correct after
    /// clock changes, system sleep/resume and every schedule rollover.
    /// </summary>
    public (DateTimeOffset NextOpening, TimeSpan Remaining) GetNextOpening(DateTimeOffset now)
    {
        foreach (var opening in GetUpcomingOpenings(now, lookaheadDays: 4))
        {
            if (opening > now)
            {
                return (opening, opening - now);
            }
        }

        // Defensive: should be unreachable with correct time zone data.
        for (int dayOffset = 5; dayOffset <= 10; dayOffset++)
        {
            var localDate = TimeZoneInfo.ConvertTime(now, SofiaTimeZone).Date.AddDays(dayOffset);
            foreach (var opening in GetOpeningsForLocalDate(localDate))
            {
                if (opening > now)
                {
                    return (opening, opening - now);
                }
            }
        }

        throw new InvalidOperationException("No future Space Rift opening could be computed.");
    }

    public TimeZoneInfo SofiaTimeZone { get; }

    /// <summary>Current instant from the injectable clock (UTC or any offset).</summary>
    public DateTimeOffset CurrentInstant => _clock();

    /// <summary>Current wall-clock time in Sofia.</summary>
    public DateTimeOffset CurrentSofiaTime => TimeZoneInfo.ConvertTime(CurrentInstant, SofiaTimeZone);

    /// <summary>
    /// True when the local Sofia date uses the daylight-saving (summer) schedule.
    /// The DST state of a transition date is evaluated at local 12:00 noon.
    /// </summary>
    public bool IsSummerSchedule(DateTime localDate)
        => SofiaTimeZone.IsDaylightSavingTime(AtNoon(localDate));
    private static DateTime AtNoon(DateTime localDate)
        => new(localDate.Year, localDate.Month, localDate.Day, 12, 0, 0, DateTimeKind.Unspecified);
}


