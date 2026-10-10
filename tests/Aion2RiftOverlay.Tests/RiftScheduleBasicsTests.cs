using System;
using System.Linq;
using Aion2RiftOverlay.Services;
using Xunit;

namespace Aion2RiftOverlay.Tests;

/// <summary>
/// Tests for time-zone resolution, per-date schedule selection and the concrete
/// opening instants generated for a Sofia date — including DST transition days.
/// Known reference dates: Bulgaria springs forward on 2025-03-30 (03:00 -> 04:00)
/// and falls back on 2025-10-26 (04:00 -> 03:00).
/// </summary>
public class RiftScheduleBasicsTests
{
    [Fact]
    public void ResolveSofiaTimeZone_PrefersWindowsIdentifier()
    {
        var tz = RiftScheduleService.ResolveSofiaTimeZone();

        Assert.Equal(TimeSpan.FromHours(2), tz.BaseUtcOffset);
        if (tz.Id != "FLE Standard Time" && tz.Id != "Europe/Sofia")
        {
            // Only the last-resort fallback would differ.
            Assert.Equal("Aion2 Sofia Fallback", tz.Id);
        }
        else
        {
            Assert.True(tz.SupportsDaylightSavingTime);
        }
    }

    [Fact]
    public void SummerSchedule_SelectedInJuly()
    {
        var service = new RiftScheduleService();

        Assert.True(service.IsSummerSchedule(new DateTime(2025, 7, 15)));

        var openings = service.GetOpeningsForLocalDate(new DateTime(2025, 7, 15));
        Assert.Equal(8, openings.Count);
        Assert.All(openings, o => Assert.Equal(TimeSpan.FromHours(3), o.Offset));
        Assert.Equal(TimeSpan.Zero, openings[0].TimeOfDay - TimeSpan.Zero);
        Assert.Equal(0, openings[0].Hour);
        Assert.Equal(21, openings[^1].Hour);
        Assert.All(openings, o => Assert.Equal(0, o.Minute));
    }

    [Fact]
    public void WinterSchedule_SelectedInJanuary()
    {
        var service = new RiftScheduleService();

        Assert.False(service.IsSummerSchedule(new DateTime(2025, 1, 15)));

        var openings = service.GetOpeningsForLocalDate(new DateTime(2025, 1, 15));
        Assert.Equal(8, openings.Count);
        Assert.All(openings, o => Assert.Equal(TimeSpan.FromHours(2), o.Offset));
        Assert.Equal(2, openings[0].Hour);
        Assert.Equal(23, openings[^1].Hour); // "23:0" interpreted as 23:00
        Assert.All(openings, o => Assert.Equal(0, o.Minute));
    }

    [Fact]
    public void WinterSchedule_HoursAreTwoFivePattern()
    {
        var service = new RiftScheduleService();
        var openings = service.GetOpeningsForLocalDate(new DateTime(2025, 11, 20));

        var expectedHours = new[] { 2, 5, 8, 11, 14, 17, 20, 23 };
        Assert.Equal(expectedHours, openings.Select(o => o.Hour));
    }

    [Fact]
    public void SummerSchedule_HoursAreZeroThreePattern()
    {
        var service = new RiftScheduleService();
        var openings = service.GetOpeningsForLocalDate(new DateTime(2025, 6, 20));

        var expectedHours = new[] { 0, 3, 6, 9, 12, 15, 18, 21 };
        Assert.Equal(expectedHours, openings.Select(o => o.Hour));
    }

    [Fact]
    public void SpringForwardDay_SkippedHour_ProducesNoOpening()
    {
        // 2025-03-30: clocks jump 03:00 EET -> 04:00 EEST, so local 03:00 does not exist.
        var service = new RiftScheduleService();
        var openings = service.GetOpeningsForLocalDate(new DateTime(2025, 3, 30));

        Assert.Equal(7, openings.Count); // 03:00 was skipped
        Assert.Equal(0, openings[0].Hour);
        Assert.Equal(TimeSpan.FromHours(2), openings[0].Offset); // 00:00 still EET
        Assert.Equal(6, openings[1].Hour);
        Assert.Equal(TimeSpan.FromHours(3), openings[1].Offset); // 06:00 EEST
        Assert.DoesNotContain(openings, o => o.Hour == 3);
    }

    [Fact]
    public void FallBackDay_KeepsFullSchedule()
    {
        // 2025-10-26: clocks go back 04:00 EEST -> 03:00 EET. No scheduled local
        // time falls inside the repeated hour, so all 8 openings exist.
        var service = new RiftScheduleService();
        var openings = service.GetOpeningsForLocalDate(new DateTime(2025, 10, 26));

        Assert.Equal(8, openings.Count);
        Assert.Equal(TimeSpan.FromHours(3), openings[0].Offset); // 02:00 still EEST
        Assert.Equal(2, openings[0].Hour);
        Assert.Equal(TimeSpan.FromHours(2), openings[1].Offset); // 05:00 already EET
        Assert.Equal(5, openings[1].Hour);
        Assert.Equal(23, openings[^1].Hour);
    }

    [Fact]
    public void TransitionDay_UsesNoonDaylightSavingState()
    {
        var service = new RiftScheduleService();

        // 12:00 on these dates is inside the new regime.
        Assert.True(service.IsSummerSchedule(new DateTime(2025, 3, 30)));
        Assert.False(service.IsSummerSchedule(new DateTime(2025, 10, 26)));
    }

    [Fact]
    public void CurrentSofiaTime_ConvertsInjectedClockToSofia()
    {
        // 10:00 UTC in July = 13:00 EEST.
        var now = new DateTimeOffset(2025, 7, 15, 10, 0, 0, TimeSpan.Zero);
        var service = new RiftScheduleService(() => now);

        var sofia = service.CurrentSofiaTime;
        Assert.Equal(13, sofia.Hour);
        Assert.Equal(TimeSpan.FromHours(3), sofia.Offset);
    }
}
