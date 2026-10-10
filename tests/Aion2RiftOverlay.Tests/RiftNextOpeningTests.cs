using System;
using Aion2RiftOverlay.Services;
using Xunit;

namespace Aion2RiftOverlay.Tests;

/// <summary>
/// Tests for <see cref="RiftScheduleService.GetNextOpening"/> using explicit
/// instants. Policy under test: an opening exactly at the current instant is the
/// "current/active" opening, so the next strictly later opening is returned.
/// </summary>
public class RiftNextOpeningTests
{
    private static RiftScheduleService CreateService(DateTimeOffset now)
        => new(() => now);

    private static DateTimeOffset Instant(int y, int mo, int d, int h, int mi, int s, double offsetHours)
        => new(y, mo, d, h, mi, s, TimeSpan.FromHours(offsetHours));

    [Fact]
    public void JustBeforeOpening_ReturnsOpeningWithOneSecond()
    {
        var now = Instant(2025, 7, 15, 14, 59, 59, 3); // EEST
        var (next, remaining) = CreateService(now).GetNextOpening(now);

        Assert.Equal(Instant(2025, 7, 15, 15, 0, 0, 3), next);
        Assert.Equal(TimeSpan.FromSeconds(1), remaining);
    }

    [Fact]
    public void ExactlyAtOpening_ReturnsNextStrictlyLaterOpening()
    {
        var now = Instant(2025, 7, 15, 15, 0, 0, 3); // EEST
        var (next, remaining) = CreateService(now).GetNextOpening(now);

        Assert.Equal(Instant(2025, 7, 15, 18, 0, 0, 3), next);
        Assert.Equal(TimeSpan.FromHours(3), remaining);
    }

    [Fact]
    public void JustAfterOpening_ReturnsNextSlot()
    {
        var now = Instant(2025, 7, 15, 15, 0, 1, 3);
        var (next, remaining) = CreateService(now).GetNextOpening(now);

        Assert.Equal(Instant(2025, 7, 15, 18, 0, 0, 3), next);
        Assert.Equal(TimeSpan.FromSeconds(2 * 3600 + 3599), remaining);
    }

    [Fact]
    public void MidnightRollover_Summer_CrossesDayBoundary()
    {
        var now = Instant(2025, 7, 15, 23, 59, 59, 3);
        var (next, remaining) = CreateService(now).GetNextOpening(now);

        Assert.Equal(Instant(2025, 7, 16, 0, 0, 0, 3), next);
        Assert.Equal(TimeSpan.FromSeconds(1), remaining);
    }

    [Fact]
    public void FinalOpeningOfDay_Summer_NextIsMidnight()
    {
        var now = Instant(2025, 7, 15, 21, 0, 0, 3);
        var (next, remaining) = CreateService(now).GetNextOpening(now);

        Assert.Equal(Instant(2025, 7, 16, 0, 0, 0, 3), next);
        Assert.Equal(TimeSpan.FromHours(3), remaining);
    }

    [Fact]
    public void FinalOpeningOfDay_Winter_NextIsNextDayTwoAm()
    {
        var now = Instant(2025, 1, 15, 23, 0, 0, 2); // EET
        var (next, remaining) = CreateService(now).GetNextOpening(now);

        Assert.Equal(Instant(2025, 1, 16, 2, 0, 0, 2), next);
        Assert.Equal(TimeSpan.FromHours(3), remaining);
    }

    [Fact]
    public void JustBeforeWinterMidnight_Opening()
    {
        var now = Instant(2025, 1, 15, 1, 59, 59, 2);
        var (next, remaining) = CreateService(now).GetNextOpening(now);

        Assert.Equal(Instant(2025, 1, 15, 2, 0, 0, 2), next);
        Assert.Equal(TimeSpan.FromSeconds(1), remaining);
    }

    [Fact]
    public void SpringForwardGap_SkippedHour_IsNotReturned()
    {
        // 2025-03-30 02:30 EET: local 03:00 does not exist (spring forward),
        // so the next valid opening is 06:00 EEST.
        var now = Instant(2025, 3, 30, 2, 30, 0, 2);
        var (next, remaining) = CreateService(now).GetNextOpening(now);

        Assert.Equal(Instant(2025, 3, 30, 6, 0, 0, 3), next);
        Assert.Equal(TimeSpan.FromMinutes(150), remaining);
    }

    [Fact]
    public void JustBeforeSpringTransition_UsesWinterSlot()
    {
        var now = Instant(2025, 3, 29, 23, 30, 0, 2);
        var (next, remaining) = CreateService(now).GetNextOpening(now);

        Assert.Equal(Instant(2025, 3, 30, 0, 0, 0, 2), next);
        Assert.Equal(TimeSpan.FromMinutes(30), remaining);
    }

    [Fact]
    public void FallBackEvening_NextOpeningAfterRepeatedHour()
    {
        var now = Instant(2025, 10, 26, 2, 30, 0, 3); // still EEST
        var (next, remaining) = CreateService(now).GetNextOpening(now);

        Assert.Equal(Instant(2025, 10, 26, 5, 0, 0, 2), next);
        Assert.Equal(TimeSpan.FromMinutes(210), remaining);
    }

    [Fact]
    public void WorksWithNonLocalMachineOffset()
    {
        // The service must handle input in any offset: 10:00 UTC+5 = 05:00 UTC
        // = 08:00 EEST, so the next summer opening is 09:00 EEST.
        var now = Instant(2025, 7, 15, 10, 0, 0, 5);
        var (next, remaining) = CreateService(now).GetNextOpening(now);

        Assert.Equal(Instant(2025, 7, 15, 9, 0, 0, 3), next);
        Assert.Equal(TimeSpan.FromHours(1), remaining);
    }

    [Fact]
    public void RecalculatesAfterOpeningPasses_NoDriftingCounter()
    {
        // Same service instance, clock advanced by the caller: the result must
        // follow the schedule rather than decrement from an earlier value.
        var current = Instant(2025, 7, 15, 14, 59, 59, 3);
        var service = CreateService(current);

        var (first, _) = service.GetNextOpening(current);
        Assert.Equal(Instant(2025, 7, 15, 15, 0, 0, 3), first);

        current = Instant(2025, 7, 15, 15, 0, 0, 3);
        var (second, secondRemaining) = service.GetNextOpening(current);
        Assert.Equal(Instant(2025, 7, 15, 18, 0, 0, 3), second);
        Assert.Equal(TimeSpan.FromHours(3), secondRemaining);
    }

    [Fact]
    public void RemainingDuration_IsAlwaysPositiveAndUnderThreeHours()
    {
        // Sample several instants across a day.
        for (int minute = 0; minute < 60; minute += 7)
        {
            var now = Instant(2025, 7, 15, 7, minute, 13, 3);
            var (_, remaining) = CreateService(now).GetNextOpening(now);

            Assert.True(remaining > TimeSpan.Zero);
            Assert.True(remaining <= TimeSpan.FromHours(3));
        }
    }
}
