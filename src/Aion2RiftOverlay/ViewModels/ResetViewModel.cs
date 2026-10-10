using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Aion2RiftOverlay.Services;

namespace Aion2RiftOverlay.ViewModels;

public enum ResetKind
{
    Daily,
    Weekly,
}

/// <summary>
/// Countdown state for a Daily/Weekly Reset overlay window. Refreshed once per
/// second from the current instant (no drifting counters).
/// </summary>
public sealed class ResetViewModel : INotifyPropertyChanged
{
    private readonly ResetScheduleService _schedule;
    private readonly ResetKind _kind;

    private string _countdownText = "--:--:--";
    private string _nextResetText = "--:--";

    public event PropertyChangedEventHandler? PropertyChanged;

    public ResetViewModel(ResetKind kind, ResetScheduleService? schedule = null, TimeZoneInfo? displayTimeZone = null)
    {
        _kind = kind;
        _schedule = schedule ?? new ResetScheduleService(displayTimeZone: displayTimeZone);
        Refresh();
    }

    /// <summary>Window title shown above the countdown.</summary>
    public string TitleText => _kind == ResetKind.Daily ? "DAILY RESET" : "WEEKLY RESET";

    public string CountdownText { get => _countdownText; private set => Set(ref _countdownText, value); }

    public string NextResetText { get => _nextResetText; private set => Set(ref _nextResetText, value); }

    public void Refresh()
    {
        var now = _schedule.CurrentInstant;
        var next = _kind == ResetKind.Daily
            ? _schedule.GetNextDailyReset(now)
            : _schedule.GetNextWeeklyReset(now);

        var seconds = Math.Max(0, (long)Math.Floor((next - now).TotalSeconds));
        var remaining = TimeSpan.FromSeconds(seconds);
        CountdownText = remaining.TotalDays >= 1
            ? $"{remaining.Days}d {remaining.Hours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}"
            : remaining.ToString(@"hh\:mm\:ss");

        NextResetText = _schedule.FormatLocal(next, _kind == ResetKind.Weekly);
    }

    /// <summary>Applies a new display zone (chosen in the main settings panel).</summary>
    public void SetDisplayTimeZone(TimeZoneInfo displayTimeZone)
    {
        _schedule.DisplayTimeZone = displayTimeZone;
        Refresh();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }
}