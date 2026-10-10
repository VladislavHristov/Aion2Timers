using System;
using Aion2RiftOverlay.Services;
using Xunit;

namespace Aion2RiftOverlay.Tests;

/// <summary>
/// Tests for the daily (07:00 UTC) and weekly (Wednesday 07:00 UTC) reset
/// schedule. All instants are computed in UTC; only display is Sofia-local
/// (10:00 EEST in summer, 09:00 EET in winter). Policy: a reset exactly at the
/// current instant counts as current; the next strictly later reset is returned.
/// </summary>
public class ResetScheduleServiceTests
{
    private static ResetScheduleService CreateService(DateTimeOffset now)
        => new(() => now);

    private static DateTimeOffset Utc(int y, int mo, int d, int h, int mi = 0, int s = 0)
        => new(y, mo, d, h, mi, s, TimeSpan.Zero);

    // --- Daily ---

    [Fact]
    public void Daily_JustBeforeReset_ReturnsSameDaySevenUtc()
    {
        var now = Utc(2025, 7, 15, 6, 59, 59);
        var next = CreateService(now).GetNextDailyReset(now);

        Assert.Equal(Utc(2025, 7, 15, 7, 0, 0), next);
    }

    [Fact]
    public void Daily_ExactlyAtReset_ReturnsNextDay()
    {
        var now = Utc(2025, 7, 15, 7, 0, 0);
        var next = CreateService(now).GetNextDailyReset(now);

        Assert.Equal(Utc(2025, 7, 16, 7, 0, 0), next);
    }

    [Fact]
    public void Daily_JustAfterReset_ReturnsNextDay()
    {
        var now = Utc(2025, 7, 15, 7, 0, 1);
        var next = CreateService(now).GetNextDailyReset(now);

        Assert.Equal(Utc(2025, 7, 16, 7, 0, 0), next);
    }

    [Fact]
    public void Daily_LateEvening_RollsOverToNextDay()
    {
        var now = Utc(2025, 7, 15, 23, 59, 59);
        var next = CreateService(now).GetNextDailyReset(now);

        Assert.Equal(Utc(2025, 7, 16, 7, 0, 0), next);
    }

    [Fact]
    public void Daily_NonUtcInputOffset_IsHandledCorrectly()
    {
        // 10:00 Sofia summer time (07:00 UTC) exactly -> next day.
        var now = new DateTimeOffset(2025, 7, 15, 10, 0, 0, TimeSpan.FromHours(3));
        var next = CreateService(now).GetNextDailyReset(now);

        Assert.Equal(Utc(2025, 7, 16, 7, 0, 0), next);
    }

    // --- Weekly ---

    [Fact]
    public void Weekly_OnTuesday_ReturnsNextDayWednesday()
    {
        var now = Utc(2025, 7, 15, 12, 0); // Tuesday
        var next = CreateService(now).GetNextWeeklyReset(now);

        Assert.Equal(Utc(2025, 7, 16, 7, 0, 0), next);
        Assert.Equal(DayOfWeek.Wednesday, next.DayOfWeek);
    }

    [Fact]
    public void Weekly_OnWednesdayBeforeResetTime_ReturnsSameWednesday()
    {
        var now = Utc(2025, 7, 16, 5, 0); // Wednesday, early morning
        var next = CreateService(now).GetNextWeeklyReset(now);

        Assert.Equal(Utc(2025, 7, 16, 7, 0, 0), next);
    }

    [Fact]
    public void Weekly_ExactlyAtWednesdayReset_ReturnsNextWeek()
    {
        var now = Utc(2025, 7, 16, 7, 0, 0); // Wednesday at reset time
        var next = CreateService(now).GetNextWeeklyReset(now);

        Assert.Equal(Utc(2025, 7, 23, 7, 0, 0), next);
    }

    [Fact]
    public void Weekly_JustAfterWednesdayReset_ReturnsNextWeek()
    {
        var now = Utc(2025, 7, 16, 7, 0, 1);
        var next = CreateService(now).GetNextWeeklyReset(now);

        Assert.Equal(Utc(2025, 7, 23, 7, 0, 0), next);
    }

    [Fact]
    public void Weekly_OnSunday_ReturnsFollowingWednesday()
    {
        var now = Utc(2025, 7, 13, 12, 0); // Sunday
        var next = CreateService(now).GetNextWeeklyReset(now);

        Assert.Equal(Utc(2025, 7, 16, 7, 0, 0), next);
    }

    [Fact]
    public void Weekly_CountdownNeverNegative_AndUnderOneWeek()
    {
        var service = CreateService(Utc(2025, 7, 14, 0, 0));
        for (int hour = 0; hour < 24; hour += 3)
        {
            var now = Utc(2025, 7, 14, hour, 17);
            var next = service.GetNextWeeklyReset(now);
            var remaining = next - now;

            Assert.True(remaining > TimeSpan.Zero);
            Assert.True(remaining <= TimeSpan.FromDays(7));
        }
    }

    // --- Sofia display conversion ---

    [Fact]
    public void FormatSofia_Summer_ShowsTenOclock()
    {
        var service = CreateService(Utc(2025, 7, 15, 0, 0));
        var formatted = service.FormatSofia(Utc(2025, 7, 15, 7, 0, 0), includeWeekday: false);

        Assert.Equal("10:00", formatted); // EEST, UTC+3
    }

    [Fact]
    public void FormatSofia_Winter_ShowsNineOclock()
    {
        var service = CreateService(Utc(2025, 1, 15, 0, 0));
        var formatted = service.FormatSofia(Utc(2025, 1, 15, 7, 0, 0), includeWeekday: false);

        Assert.Equal("09:00", formatted); // EET, UTC+2
    }

    [Fact]
    public void FormatSofia_Weekly_IncludesWeekday()
    {
        var service = CreateService(Utc(2025, 7, 15, 0, 0));
        var formatted = service.FormatSofia(Utc(2025, 7, 16, 7, 0, 0), includeWeekday: true);

        Assert.Equal("Wed 10:00", formatted);
    }

    // --- DST transition week (autumn 2025: Sunday Oct 26, 04:00 EEST -> 03:00 EET) ---

    [Fact]
    public void Weekly_SpanningDstFallBack_KeepsUtcInstant()
    {
        // Wednesday Oct 29, 2025 is after the DST change; 07:00 UTC = 09:00 EET.
        var now = Utc(2025, 10, 27, 12, 0); // Monday
        var next = CreateService(now).GetNextWeeklyReset(now);

        Assert.Equal(Utc(2025, 10, 29, 7, 0, 0), next);
        Assert.Equal("09:00", CreateService(now).FormatSofia(next, includeWeekday: false));
    }
}