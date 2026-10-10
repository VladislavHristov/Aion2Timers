using System;
using System.Collections.Generic;

namespace Aion2RiftOverlay.Services;

/// <summary>
/// Resolves the user-selected DISPLAY time zone. The rift/reset schedule instants
/// themselves are always computed from the Sofia schedule / UTC; this zone only
/// controls how those instants are displayed (so people outside Bulgaria can see
/// the times in their own local time).
/// </summary>
public static class TimeZoneService
{
    /// <summary>All time zones known to the OS, sorted by UTC offset then name.</summary>
    public static IReadOnlyCollection<TimeZoneInfo> GetSystemTimeZones()
        => TimeZoneInfo.GetSystemTimeZones();

    /// <summary>
    /// Resolves a settings-stored zone id; empty/unknown ids fall back to the
    /// Sofia zone (the app's default display zone).
    /// </summary>
    public static TimeZoneInfo Resolve(string? id)
    {
        if (!string.IsNullOrWhiteSpace(id))
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

        return RiftScheduleService.ResolveSofiaTimeZone();
    }
}