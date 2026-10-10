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
    public void Sanitize()
    {
        OpacityPercent = Math.Clamp(double.IsFinite(OpacityPercent) ? OpacityPercent : 85, 20, 100);
        if (!double.IsFinite(WindowX) || !double.IsFinite(WindowY))
        {
            WindowX = 0;
            WindowY = 0;
            HasWindowPosition = false;
        }
    }
}

