using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Aion2RiftOverlay.Models;
using Aion2RiftOverlay.Services;

namespace Aion2RiftOverlay.ViewModels;

/// <summary>
/// Presentation state for the overlay. The window calls <see cref="Refresh"/>
/// roughly once per second; the countdown is always recomputed from the current
/// time so it never drifts and survives sleep/resume.
/// </summary>
public sealed class MainViewModel : INotifyPropertyChanged
{
    /// <summary>Alert kinds raised via <see cref="AlertRequested"/>.</summary>
    public const string AlertKindTenMinute = "ten-minute";
    public const string AlertKindOpening = "opening";

    private static readonly TimeSpan TenMinuteThreshold = TimeSpan.FromMinutes(10);

    private readonly RiftScheduleService _schedule;
    private readonly SettingsService _settingsService;
    private readonly AppSettings _settings;

    private string _countdownText = "--:--:--";
    private string _nextOpeningText = "--:--";
    private string _statusText = string.Empty;

    // Voice-alert state machine: which opening we are counting down to and
    // whether its 10-minute warning has already fired.
    private DateTimeOffset? _armedOpening;
    private DateTimeOffset? _warnedFor;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Raised when the reliable "close app" action is requested.</summary>
    public event EventHandler? RequestClose;

    /// <summary>Raised when the overlay should move back to a safe centered position.</summary>
    public event EventHandler? RequestResetPosition;

    /// <summary>Raised with <see cref="AlertKindTenMinute"/> or <see cref="AlertKindOpening"/>.
    /// The UI maps the kind to a sound file and plays it.</summary>
    public event EventHandler<string>? AlertRequested;

    public RelayCommand ToggleSettingsCommand { get; }
    public RelayCommand ResetPositionCommand { get; }
    public RelayCommand CloseCommand { get; }

    public MainViewModel()
        : this(schedule: null, settingsService: null)
    {
    }

    public MainViewModel(RiftScheduleService? schedule, SettingsService? settingsService)
    {
        _schedule = schedule ?? new RiftScheduleService();
        _settingsService = settingsService ?? new SettingsService();
        _settings = _settingsService.Load();

        ToggleSettingsCommand = new RelayCommand(() => SettingsPanelOpen = !SettingsPanelOpen);
        ResetPositionCommand = new RelayCommand(() => RequestResetPosition?.Invoke(this, EventArgs.Empty));
        CloseCommand = new RelayCommand(() => RequestClose?.Invoke(this, EventArgs.Empty));

        Refresh();
    }

    public string CountdownText { get => _countdownText; private set => Set(ref _countdownText, value); }

    public string NextOpeningText { get => _nextOpeningText; private set => Set(ref _nextOpeningText, value); }

    public string StatusText { get => _statusText; private set => Set(ref _statusText, value); }

    public bool IsTopMost
    {
        get => _settings.AlwaysOnTop;
        set
        {
            if (_settings.AlwaysOnTop == value) return;
            _settings.AlwaysOnTop = value;
            SaveAndNotify(nameof(IsTopMost));
        }
    }

    public bool IsLocked
    {
        get => _settings.LockPosition;
        set
        {
            if (_settings.LockPosition == value) return;
            _settings.LockPosition = value;
            SaveAndNotify(nameof(IsLocked));
        }
    }

    public bool MinimizeToTray
    {
        get => _settings.MinimizeToTray;
        set
        {
            if (_settings.MinimizeToTray == value) return;
            _settings.MinimizeToTray = value;
            SaveAndNotify(nameof(MinimizeToTray));
        }
    }

    public bool SettingsPanelOpen
    {
        get => _settings.SettingsPanelOpen;
        set
        {
            if (_settings.SettingsPanelOpen == value) return;
            _settings.SettingsPanelOpen = value;
            SaveAndNotify(nameof(SettingsPanelOpen));
        }
    }

    /// <summary>Voice alerts: 10-minute warning + opening sound.</summary>
    public bool VoiceAlertsEnabled
    {
        get => _settings.VoiceAlertsEnabled;
        set
        {
            if (_settings.VoiceAlertsEnabled == value) return;
            _settings.VoiceAlertsEnabled = value;
            SaveAndNotify(nameof(VoiceAlertsEnabled));
        }
    }

    /// <summary>Backdrop opacity percent (20..100). Text stays fully opaque.</summary>
    public double OpacityPercent
    {
        get => _settings.OpacityPercent;
        set
        {
            var clamped = Math.Round(Math.Clamp(value, 20, 100));
            if (Math.Abs(_settings.OpacityPercent - clamped) < 0.5) return;
            _settings.OpacityPercent = clamped;
            SaveAndNotify(nameof(OpacityPercent));
        }
    }

    /// <summary>Sound file for the 10-minute warning voice clip.</summary>
    public string TenMinuteSoundFile
        => ResolveSoundFile(_settings.TenMinuteWarningFile, "10minutes.mp3", SoundAlertService.OverridesDirectory, SoundAlertService.SoundsDirectory);

    /// <summary>Sound file played when a rift opens.</summary>
    public string OpeningSoundFile
        => ResolveSoundFile(_settings.OpeningSoundFile, "OpenNow.mp3", SoundAlertService.OverridesDirectory, SoundAlertService.SoundsDirectory);

    /// <summary>Saved window position, if a valid one exists.</summary>
    public bool TryGetSavedPosition(out double x, out double y)
    {
        x = _settings.WindowX;
        y = _settings.WindowY;
        return _settings.HasWindowPosition
            && double.IsFinite(x)
            && double.IsFinite(y);
    }

    public void SaveWindowPosition(double x, double y)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y))
        {
            return;
        }

        _settings.HasWindowPosition = true;
        _settings.WindowX = x;
        _settings.WindowY = y;
        SaveSettings();
    }

    public void SaveSettings() => _settingsService.Save(_settings);

    /// <summary>
    /// Recomputes the countdown and labels from the current instant. Called once
    /// per second by the UI timer, which also covers system sleep/resume and
    /// clock changes. Also drives the voice-alert state machine.
    /// </summary>
    public void Refresh()
    {
        var now = _schedule.CurrentInstant;
        var (next, remaining) = _schedule.GetNextOpening(now);

        var seconds = Math.Max(0, (long)Math.Floor(remaining.TotalSeconds));
        CountdownText = TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss");

        var localNext = TimeZoneInfo.ConvertTime(next, _schedule.SofiaTimeZone);
        NextOpeningText = localNext.ToString("HH:mm");

        var isSummer = _schedule.IsSummerSchedule(localNext.Date);
        StatusText = isSummer ? "EEST - SUMMER" : "EET - WINTER";

        EvaluateAlerts(now, next);
    }

    /// <summary>
    /// Voice-alert state machine, evaluated on every refresh:
    /// - when the opening we are counting down to arrives, fire the opening alert;
    /// - re-arm for the next scheduled opening;
    /// - fire the 10-minute warning once per opening when remaining <= 10 min.
    /// Sleep/resume safe: everything is derived from the current instant.
    /// </summary>
    internal void EvaluateAlerts(DateTimeOffset now, DateTimeOffset nextOpening)
    {
        if (!VoiceAlertsEnabled)
        {
            _armedOpening = null;
            _warnedFor = null;
            return;
        }

        // The opening we were counting down to has arrived (or was passed while
        // the system was asleep).
        if (_armedOpening.HasValue && now >= _armedOpening.Value)
        {
            AlertRequested?.Invoke(this, AlertKindOpening);
            _warnedFor = null;
            _armedOpening = null;
        }

        if (!_armedOpening.HasValue)
        {
            _armedOpening = nextOpening;
        }
        else if (nextOpening != _armedOpening.Value)
        {
            // The countdown target rolled over without the exact instant being
            // observed; re-arm for the new opening.
            _armedOpening = nextOpening;
            _warnedFor = null;
        }

        if (_warnedFor != nextOpening && nextOpening - now <= TenMinuteThreshold)
        {
            _warnedFor = nextOpening;
            AlertRequested?.Invoke(this, AlertKindTenMinute);
        }
    }

    /// <summary>
    /// Sound resolution priority (highest first):
    ///   1. Path configured in settings.json (only if the file still exists)
    ///   2. "sounds" folder next to the exe - user overrides, no recompile needed
    ///   3. Embedded defaults extracted to %LOCALAPPDATA%\Aion2RiftOverlay\sounds
    /// Returns an empty string when nothing exists, so alerts stay silent.
    /// </summary>
    internal static string ResolveSoundFile(
        string configured,
        string defaultFileName,
        string overrideDirectory,
        string fallbackDirectory)
    {
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
        {
            return configured;
        }

        var overridePath = Path.Combine(overrideDirectory, defaultFileName);
        if (File.Exists(overridePath))
        {
            return overridePath;
        }

        var fallbackPath = Path.Combine(fallbackDirectory, defaultFileName);
        return File.Exists(fallbackPath) ? fallbackPath : string.Empty;
    }

    private void SaveAndNotify(string propertyName)
    {
        SaveSettings();
        OnPropertyChanged(propertyName);
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
