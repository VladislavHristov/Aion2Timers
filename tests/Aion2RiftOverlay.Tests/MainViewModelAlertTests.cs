using System;
using System.Collections.Generic;
using System.IO;
using Aion2RiftOverlay.Services;
using Aion2RiftOverlay.ViewModels;
using Xunit;

namespace Aion2RiftOverlay.Tests;

/// <summary>
/// Tests for the voice-alert state machine in <see cref="MainViewModel"/>:
/// a "ten-minute" alert once when remaining time drops to 10 minutes or less,
/// and an "opening" alert when the armed opening instant arrives. A mutable
/// clock lets tests advance time and prove alerts fire exactly once per opening.
/// Note: the constructor evaluates alerts immediately, so view models are always
/// created at a moment with no alert pending, and the test subscribes before
/// advancing the clock.
/// </summary>
public class MainViewModelAlertTests
{
    private DateTimeOffset _now;
    private readonly List<string> _alerts = new();
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "Aion2RiftOverlayTests", Guid.NewGuid().ToString("N"));

    private MainViewModel CreateViewModel()
    {
        Directory.CreateDirectory(_tempDir);
        var schedule = new RiftScheduleService(() => _now);
        var settings = new SettingsService(_tempDir);
        var vm = new MainViewModel(schedule, settings);
        vm.AlertRequested += (_, kind) => _alerts.Add(kind);
        return vm;
    }

    private void AdvanceTo(int hour, int minute, int second = 0)
    {
        _now = new DateTimeOffset(2025, 7, 15, hour, minute, second, TimeSpan.FromHours(3)); // EEST
    }

    [Fact]
    public void NoAlert_WhenMoreThanTenMinutesRemain()
    {
        AdvanceTo(14, 40); // 20 min before the 15:00 opening
        var vm = CreateViewModel();

        Assert.Empty(_alerts);
    }

    [Fact]
    public void TenMinuteWarning_Fires_WhenRemainingDropsToTenMinutes()
    {
        AdvanceTo(14, 40);
        var vm = CreateViewModel();

        AdvanceTo(14, 50); // exactly 10:00 remaining
        vm.Refresh();

        Assert.Equal(new[] { MainViewModel.AlertKindTenMinute }, _alerts);
    }

    [Fact]
    public void TenMinuteWarning_FiresOnlyOnce()
    {
        AdvanceTo(14, 40);
        var vm = CreateViewModel();

        AdvanceTo(14, 50);
        vm.Refresh();
        vm.Refresh();
        vm.Refresh();

        Assert.Equal(1, _alerts.Count);
        Assert.Equal(MainViewModel.AlertKindTenMinute, _alerts[0]);
    }

    [Fact]
    public void OpeningAlert_Fires_AtOpeningInstant()
    {
        AdvanceTo(14, 40);
        var vm = CreateViewModel();

        AdvanceTo(15, 0, 0);
        vm.Refresh();

        Assert.Equal(new[] { MainViewModel.AlertKindOpening }, _alerts);
    }

    [Fact]
    public void FullCycle_WarnsOnce_OpeningOnce_ReArmsForNextOpening()
    {
        AdvanceTo(14, 40);
        var vm = CreateViewModel();

        AdvanceTo(14, 55);
        vm.Refresh(); // ten-minute warning

        AdvanceTo(15, 0, 0);
        vm.Refresh(); // opening

        AdvanceTo(15, 30);
        vm.Refresh(); // nothing

        AdvanceTo(17, 50);
        vm.Refresh(); // next warning (18:00 opening)

        Assert.Equal(
            new[]
            {
                MainViewModel.AlertKindTenMinute,
                MainViewModel.AlertKindOpening,
                MainViewModel.AlertKindTenMinute,
            },
            _alerts);
    }

    [Fact]
    public void OpeningAlert_FiresLate_AfterSleepOrResume()
    {
        AdvanceTo(14, 40);
        var vm = CreateViewModel();

        AdvanceTo(14, 55);
        vm.Refresh(); // arms the 15:00 opening

        // System slept through the opening; next refresh is after it.
        AdvanceTo(15, 20);
        vm.Refresh();

        // Waking at 14:55 still legitimately warns first; then the opening fires late.
        Assert.Equal(new[] { MainViewModel.AlertKindTenMinute, MainViewModel.AlertKindOpening }, _alerts);
    }

    [Fact]
    public void NoStaleAlerts_WhenAppStartsAfterTheOpening()
    {
        AdvanceTo(15, 20); // fresh start after the 15:00 opening
        var vm = CreateViewModel();

        Assert.Empty(_alerts); // nothing armed -> no opening sound, no warning
    }

    [Fact]
    public void DisabledAlerts_NeverFire()
    {
        AdvanceTo(14, 40);
        var vm = CreateViewModel();
        vm.VoiceAlertsEnabled = false;

        vm.Refresh();
        AdvanceTo(14, 55);
        vm.Refresh();
        AdvanceTo(15, 0, 0);
        vm.Refresh();
        AdvanceTo(15, 0, 5);
        vm.Refresh();

        Assert.Empty(_alerts);
    }
}
