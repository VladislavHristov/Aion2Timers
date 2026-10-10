using System;
using System.Globalization;

namespace Aion2RiftOverlay.Services;

/// <summary>
/// Daily and weekly reset schedule for AION 2.
///
/// Both resets happen at 07:00 UTC:
///   - Daily: every day at 07:00 UTC (10:00 Sofia time during EEST/UTC+3,
///     09:00 during EET/UTC+2 - DST handled by the time-zone database).
///   - Weekly: every Wednesday at the same 07:00 UTC instant.
///
/// Policy matches RiftScheduleService: a reset exactly at the current instant
/// counts as the current one; the next strictly later reset is returned.
/// Instants are computed in UTC and only DISPLAYED converted to Sofia time.
/// </summary>
public sealed class ResetScheduleService
{
    public static readonly TimeSpan ResetUtcTime = new(7, 0, 0);
    public const DayOfWeek WeeklyResetDay = DayOfWeek.Wednesday;

    private readonly TimeZoneInfo _sofiaTimeZone;
    private readonly Func<DateTimeOffset> _clock;

    public ResetScheduleService(Func<DateTimeOffset>? clock = null, TimeZoneInfo? sofiaTimeZone = null)
    {
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _sofiaTimeZone = sofiaTimeZone ?? RiftScheduleService.ResolveSofiaTimeZone();
    }

    public TimeZoneInfo SofiaTimeZone => _sofiaTimeZone;

    /// <summary>Current instant from the injectable clock.</summary>
    public DateTimeOffset CurrentInstant => _clock();

    /// <summary>Next daily reset (07:00 UTC) strictly after <paramref name="now"/>.</summary>
    public DateTimeOffset GetNextDailyReset(DateTimeOffset now)
    {
        var candidate = now.UtcDateTime.Date.Add(ResetUtcTime);
        if (candidate <= now)
        {
            candidate = candidate.AddDays(1);
        }

        return ToUtcOffset(candidate);
    }

    /// <summary>
    /// Next weekly reset (Wednesday 07:00 UTC) strictly after <paramref name="now"/>.
    /// A Wednesday 07:00 UTC instant equal to "now" rolls to the next Wednesday.
    /// </summary>
    public DateTimeOffset GetNextWeeklyReset(DateTimeOffset now)
    {
        int daysUntilResetDay = ((int)WeeklyResetDay - (int)now.UtcDateTime.DayOfWeek + 7) % 7;
        var candidate = now.UtcDateTime.Date.AddDays(daysUntilResetDay).Add(ResetUtcTime);
        if (candidate <= now)
        {
            candidate = candidate.AddDays(7);
        }

        return ToUtcOffset(candidate);
    }

    /// <summary>
    /// Formats a reset instant in Sofia local time; the weekly variant includes
    /// the weekday abbreviation (e.g. "Wed 10:00").
    /// </summary>
    public string FormatSofia(DateTimeOffset resetUtc, bool includeWeekday)
    {
        var local = TimeZoneInfo.ConvertTime(resetUtc, _sofiaTimeZone);
        return includeWeekday ? local.ToString("ddd HH:mm", CultureInfo.InvariantCulture) : local.ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    private static DateTimeOffset ToUtcOffset(DateTime utcCandidate)
        => new(utcCandidate.Ticks, TimeSpan.Zero);
}
