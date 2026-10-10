using System;

namespace Aion2RiftOverlay.Models;

/// <summary>
/// User preferences persisted as JSON under the user's LocalApplicationData folder.
/// Contains no personal information beyond window placement and UI preferences.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Backdrop opacity in percent (20..100). Foreground text stays fully opaque.</summary>
    public double OpacityPercent { get; set; } = 85;

    /// <summary>Keep the overlay on top of other windows. Default: true.</summary>
    public bool AlwaysOnTop { get; set; } = true;

    /// <summary>Prevent the window from being dragged.</summary>
    public bool LockPosition { get; set; }

    /// <summary>Minimizing hides the window to the system tray instead of the taskbar.</summary>
    public bool MinimizeToTray { get; set; }

    /// <summary>Whether the settings panel is expanded.</summary>
    public bool SettingsPanelOpen { get; set; }

    /// <summary>True when a valid window position was saved previously.</summary>
    public bool HasWindowPosition { get; set; }

    /// <summary>Saved window X position in device-independent pixels (DIPs).</summary>
    public double WindowX { get; set; }

    /// <summary>Saved window Y position in device-independent pixels (DIPs).</summary>
    public double WindowY { get; set; }

    /// <summary>
    /// Normalizes values that may come from a malformed or hand-edited file so the UI
    /// always receives safe values.
    /// </summary>
    /// <summary>Play a voice clip 10 minutes before each opening and at the opening time.</summary>
    public bool VoiceAlertsEnabled { get; set; } = true;

    /// <summary>Sound file for the 10-minute warning. Empty = bundled default path.</summary>
    public string TenMinuteWarningFile { get; set; } = string.Empty;

    /// <summary>Sound file for the opening alert. Empty = bundled default path.</summary>
    public string OpeningSoundFile { get; set; } = string.Empty;
    /// <summary>Id of the time zone used to DISPLAY schedule times. Empty = Sofia (default).</summary>
    public string DisplayTimeZoneId { get; set; } = string.Empty;

    /// <summary>Show the Daily Reset overlay window.</summary>
    public bool ShowDailyResetOverlay { get; set; } = true;

    /// <summary>Show the Weekly Reset overlay window.</summary>
    public bool ShowWeeklyResetOverlay { get; set; } = true;

    /// <summary>Saved position of the Daily Reset window.</summary>
    public bool HasDailyWindowPosition { get; set; }
    public double DailyWindowX { get; set; }
    public double DailyWindowY { get; set; }

    /// <summary>Saved position of the Weekly Reset window.</summary>
    public bool HasWeeklyWindowPosition { get; set; }
    public double WeeklyWindowX { get; set; }
    public double WeeklyWindowY { get; set; }
        public void Sanitize()
    {
        OpacityPercent = Math.Clamp(double.IsFinite(OpacityPercent) ? OpacityPercent : 85, 20, 100);
        if (!double.IsFinite(WindowX) || !double.IsFinite(WindowY))
        {
            WindowX = 0;
            WindowY = 0;
            HasWindowPosition = false;
        }
        if (!double.IsFinite(DailyWindowX) || !double.IsFinite(DailyWindowY))
        {
            DailyWindowX = 0;
            DailyWindowY = 0;
            HasDailyWindowPosition = false;
        }
        if (!double.IsFinite(WeeklyWindowX) || !double.IsFinite(WeeklyWindowY))
        {
            WeeklyWindowX = 0;
            WeeklyWindowY = 0;
            HasWeeklyWindowPosition = false;
        }
    }
}





