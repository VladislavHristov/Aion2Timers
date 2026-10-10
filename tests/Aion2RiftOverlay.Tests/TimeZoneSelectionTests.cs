using System;
using System.IO;
using Aion2RiftOverlay.Services;
using Aion2RiftOverlay.ViewModels;
using Xunit;

namespace Aion2RiftOverlay.Tests;

/// <summary>
/// Tests for the user-selectable DISPLAY time zone. The schedule instants are
/// always computed from the Sofia rift schedule / UTC reset times; selecting a
/// display zone only changes how those instants are rendered (NextOpeningText,
/// reset window times). Settings persist the zone id; empty/unknown falls back
/// to Sofia.
/// </summary>
public class TimeZoneSelectionTests
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "Aion2RiftOverlayTests", Guid.NewGuid().ToString("N"));

    private static DateTimeOffset Utc(int y, int mo, int d, int h, int mi = 0, int s = 0)
        => new(y, mo, d, h, mi, s, TimeSpan.Zero);

    private MainViewModel CreateViewModel(DateTimeOffset now, string? settingsJson = null)
    {
        Directory.CreateDirectory(_tempDir);
        var schedule = new RiftScheduleService(() => now);
        var settings = new SettingsService(_tempDir);
        var vm = new MainViewModel(schedule, settings);
        return vm;
    }

    // --- TimeZoneService.Resolve ---

    [Fact]
    public void Resolve_EmptyOrUnknownId_FallsBackToSofia()
    {
        var sofia = RiftScheduleService.ResolveSofiaTimeZone();

        Assert.Equal(sofia.Id, TimeZoneService.Resolve(null).Id);
        Assert.Equal(sofia.Id, TimeZoneService.Resolve("").Id);
        Assert.Equal(sofia.Id, TimeZoneService.Resolve("Not/ARealZone").Id);
    }

    [Fact]
    public void Resolve_KnownId_ReturnsThatZone()
    {
        var resolved = TimeZoneService.Resolve(TimeZoneInfo.Utc.Id);

        Assert.Equal(TimeZoneInfo.Utc.Id, resolved.Id);
    }

    [Fact]
    public void GetSystemTimeZones_ContainsUtc()
    {
        Assert.Contains(TimeZoneService.GetSystemTimeZones(), z => z.Id == TimeZoneInfo.Utc.Id);
    }

    // --- MainViewModel display conversion ---

    [Fact]
    public void DefaultDisplayZone_IsSofia()
    {
        // 2025-07-15 10:00 UTC -> next opening 12:00 UTC = 15:00 EEST (Sofia, +3).
        var now = Utc(2025, 7, 15, 10, 0, 0);
        var vm = CreateViewModel(now);

        Assert.Equal(RiftScheduleService.ResolveSofiaTimeZone().Id, vm.DisplayTimeZone.Id);
        Assert.Equal("15:00", vm.NextOpeningText);
    }

    [Fact]
    public void SelectingUtcZone_ShowsSofiaScheduleInstant_InUtc()
    {
        var now = Utc(2025, 7, 15, 10, 0, 0);
        var vm = CreateViewModel(now);

        vm.SelectedTimeZone = TimeZoneInfo.Utc;

        Assert.Equal(TimeZoneInfo.Utc.Id, vm.DisplayTimeZone.Id);
        Assert.Equal("12:00", vm.NextOpeningText);
    }

    [Fact]
    public void SelectingSameZone_DoesNotReSaveOrReset()
    {
        var now = Utc(2025, 7, 15, 10, 0, 0);
        var vm = CreateViewModel(now);

        vm.SelectedTimeZone = vm.SelectedTimeZone; // no-op

        Assert.Equal("15:00", vm.NextOpeningText);
    }

    [Fact]
    public void SelectedZone_PersistsInSettings_AndIsRestored()
    {
        var now = Utc(2025, 7, 15, 10, 0, 0);
        var vm = CreateViewModel(now);
        vm.SelectedTimeZone = TimeZoneInfo.Utc;
        vm.SaveSettings();

        var schedule = new RiftScheduleService(() => now);
        var settings = new SettingsService(_tempDir);
        var vm2 = new MainViewModel(schedule, settings);

        Assert.Equal(TimeZoneInfo.Utc.Id, vm2.DisplayTimeZone.Id);
        Assert.Equal("12:00", vm2.NextOpeningText);
    }

    // --- ResetScheduleService display zone ---

    [Fact]
    public void Reset_FormatLocal_UsesDisplayZone()
    {
        var now = Utc(2025, 7, 15, 10, 0, 0);
        var schedule = new ResetScheduleService(() => now, TimeZoneInfo.Utc);
        var next = schedule.GetNextDailyReset(now);

        Assert.Equal("07:00", schedule.FormatLocal(next, includeWeekday: false));
        Assert.EndsWith(" 07:00", schedule.FormatLocal(next, includeWeekday: true));
    }

    [Fact]
    public void Reset_DisplayTimeZoneSetter_FallsBackToSofiaOnNull()
    {
        var now = Utc(2025, 7, 15, 10, 0, 0);
        var schedule = new ResetScheduleService(() => now, TimeZoneInfo.Utc)
        {
            DisplayTimeZone = null!,
        };

        Assert.Equal(RiftScheduleService.ResolveSofiaTimeZone().Id, schedule.DisplayTimeZone.Id);
    }

    // --- ResetViewModel display zone ---

    [Fact]
    public void ResetViewModel_SetDisplayTimeZone_RefreshesTimes()
    {
        var now = Utc(2025, 7, 15, 10, 0, 0);
        var schedule = new ResetScheduleService(() => now, TimeZoneInfo.Utc);
        var vm = new ResetViewModel(ResetKind.Daily, schedule);

        Assert.Equal("07:00", vm.NextResetText);

        // Switch the display to Sofia: the 07:00 UTC reset shows as 10:00 EEST.
        vm.SetDisplayTimeZone(RiftScheduleService.ResolveSofiaTimeZone());

        Assert.Equal("10:00", vm.NextResetText);
    }
}