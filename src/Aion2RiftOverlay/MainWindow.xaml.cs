using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Aion2RiftOverlay.ViewModels;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;
using Win32 = Microsoft.Win32;

namespace Aion2RiftOverlay;

/// <summary>
/// Frameless, draggable overlay window. Owns window mechanics (dragging,
/// position restore/clamping, tray icon, timers); display state lives in
/// <see cref="MainViewModel"/>.
/// </summary>
public partial class MainWindow : Window
{
    private const double EstimatedHeight = 158; // used for pre-layout clamping

    private readonly MainViewModel _vm;
    private readonly DispatcherTimer _uiTimer;
    private readonly DispatcherTimer _positionSaveTimer;
    private readonly Forms.NotifyIcon _notifyIcon;
    private ResetWindow? _dailyResetWindow;
    private ResetWindow? _weeklyResetWindow;
    private bool _positionRestored;
    private readonly Services.SoundAlertService _soundAlerts = new Services.SoundAlertService();

    public MainWindow()
    {
        // Extract embedded alert sounds before the view model resolves their paths.
        _soundAlerts.ExtractDefaultSounds();
        _vm = new MainViewModel();
        _vm.RequestClose += (_, _) => Close();
        _vm.RequestResetPosition += (_, _) => ResetPosition();
        _vm.PropertyChanged += OnViewModelPropertyChanged;
        _vm.AlertRequested += OnAlertRequested;

        InitializeComponent();
        DataContext = _vm;

        _uiTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        _uiTimer.Tick += (_, _) => _vm.Refresh();
        _uiTimer.Start();

        // Debounced position save while dragging.
        _positionSaveTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        _positionSaveTimer.Tick += (_, _) =>
        {
            _positionSaveTimer.Stop();
            _vm.SaveWindowPosition(Left, Top);
        };

        ApplyBackdropOpacity();
        _notifyIcon = CreateNotifyIcon();
        Loaded += (_, _) => { ClampIntoWorkArea(); UpdateResetOverlayVisibility(); };
    }

    // --- Window lifecycle ---------------------------------------------------

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        RestorePosition();
        Win32.SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    protected override void OnClosed(EventArgs e)
    {
        Win32.SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        _uiTimer.Stop();
        _positionSaveTimer.Stop();
        _vm.SaveWindowPosition(Left, Top);

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();

        base.OnClosed(e);
    }

    // --- Dragging -----------------------------------------------------------

    private void RootBorder_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Interactive controls (buttons, sliders, checkboxes) handle their own
        // mouse-down and mark it handled, so this only fires on background areas.
        if (_vm.IsLocked || e.Handled || e.ClickCount != 1)
        {
            return;
        }

        try
        {
            DragMove();
            _vm.SaveWindowPosition(Left, Top);
        }
        catch (InvalidOperationException)
        {
            // Mouse button was already released; harmless.
        }
    }

    // --- Buttons ------------------------------------------------------------

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.MinimizeToTray)
        {
            Hide();
        }
        else
        {
            WindowState = WindowState.Minimized;
        }
    }

    // --- Position handling ----------------------------------------------------

    private void RestorePosition()
    {
        if (_vm.TryGetSavedPosition(out var x, out var y))
        {
            Left = x;
            Top = y;
        }
        else
        {
            CenterOnScreen();
        }

        _positionRestored = true;
        ClampIntoWorkArea();
    }

    private void ResetPosition()
    {
        CenterOnScreen();
        ClampIntoWorkArea();
        _vm.SaveWindowPosition(Left, Top);
    }

    private void CenterOnScreen()
    {
        var (x, y, w, h) = GetWorkAreaDip(Forms.Screen.PrimaryScreen!);
        Left = x + Math.Max(0, (w - Width) / 2);
        Top = y + Math.Max(0, (h - EstimatedHeight) / 2);
    }

    /// <summary>
    /// Keeps the window fully inside the visible desktop work area, so restored
    /// or dragged positions never end up off-screen after monitor or resolution
    /// changes.
    /// </summary>
    private void ClampIntoWorkArea()
    {
        if (!_positionRestored)
        {
            return;
        }

        try
        {
            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget is null)
            {
                return;
            }

            var width = ActualWidth > 0 ? ActualWidth : Width;
            var height = ActualHeight > 0 ? ActualHeight : EstimatedHeight;

            var (ax, ay, aw, ah) = GetWorkAreaDip(Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle));
            Left = Math.Clamp(Left, ax, Math.Max(ax, ax + aw - width));
            Top = Math.Clamp(Top, ay, Math.Max(ay, ay + ah - height));
        }
        catch (Exception ex) when (ex is InvalidOperationException or OverflowException)
        {
            // Window not shown yet or screen data unavailable: keep current values.
        }
    }

    /// <summary>Converts a screen's working area from device pixels into DIPs.</summary>
    private (double X, double Y, double W, double H) GetWorkAreaDip(Forms.Screen screen)
    {
        var area = screen.WorkingArea;
        try
        {
            var fromDevice = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice;
            if (fromDevice.HasValue)
            {
                var tl = fromDevice.Value.Transform(new System.Windows.Point(area.Left, area.Top));
                var br = fromDevice.Value.Transform(new System.Windows.Point(area.Right, area.Bottom));
                return (tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y);
            }
        }
        catch (InvalidOperationException)
        {
        }

        // Fallback: assume 96 DPI.
        return (area.Left, area.Top, area.Width, area.Height);
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e) => ClampIntoWorkArea();


    // --- Opacity ------------------------------------------------------------

    private void UpdateResetOverlayVisibility()
    {
        SetResetOverlayVisible(ResetKind.Daily, _vm.ShowDailyResetOverlay);
        SetResetOverlayVisible(ResetKind.Weekly, _vm.ShowWeeklyResetOverlay);
    }

    private void SetResetOverlayVisible(ResetKind kind, bool visible)
    {
        if (kind == ResetKind.Daily)
        {
            _dailyResetWindow ??= new ResetWindow(kind, _vm);
            _dailyResetWindow.SetOverlayVisible(visible);
        }
        else
        {
            _weeklyResetWindow ??= new ResetWindow(kind, _vm);
            _weeklyResetWindow.SetOverlayVisible(visible);
        }
    }
    private void OnAlertRequested(object? sender, string kind)
    {
        var file = kind == MainViewModel.AlertKindOpening
            ? _vm.OpeningSoundFile
            : _vm.TenMinuteSoundFile;
        _soundAlerts.Play(file);
    }
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.OpacityPercent))
        {
            ApplyBackdropOpacity();
            _dailyResetWindow?.SyncBackdropOpacity();
            _weeklyResetWindow?.SyncBackdropOpacity();
        }
        else if (e.PropertyName == nameof(MainViewModel.IsTopMost))
        {
            if (_dailyResetWindow is not null) { _dailyResetWindow.Topmost = _vm.IsTopMost; }
            if (_weeklyResetWindow is not null) { _weeklyResetWindow.Topmost = _vm.IsTopMost; }
        }
        else if (e.PropertyName == nameof(MainViewModel.ShowDailyResetOverlay) ||
                 e.PropertyName == nameof(MainViewModel.ShowWeeklyResetOverlay))
        {
            UpdateResetOverlayVisibility();
        }
    }

    /// <summary>
    /// Opacity is applied to the translucent backdrop only. Foreground text keeps
    /// full opacity, which keeps the countdown readable even at 20%.
    /// </summary>
    private void ApplyBackdropOpacity()
    {
        var alpha = (byte)Math.Round(255 * Math.Clamp(_vm.OpacityPercent, 20, 100) / 100.0);
        RootBorder.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromArgb(alpha, 0x0C, 0x14, 0x1E));
    }

    // --- System tray ----------------------------------------------------------

    private Forms.NotifyIcon CreateNotifyIcon()
    {
        var icon = new Forms.NotifyIcon
        {
            Text = "AION 2 Space Rift Overlay",
            Visible = true,
            Icon = GetAppIcon(),
        };

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Show overlay", null, (_, _) => RestoreFromTray());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Close());
        icon.ContextMenuStrip = menu;
        icon.DoubleClick += (_, _) => RestoreFromTray();

        return icon;
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private static Drawing.Icon GetAppIcon()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath))
            {
                var icon = Drawing.Icon.ExtractAssociatedIcon(exePath);
                if (icon is not null)
                {
                    return icon;
                }
            }
        }
        catch (Exception ex) when (ex is System.IO.IOException or ArgumentException)
        {
        }

        // Fallback: simple cyan disc.
        using var bitmap = new Drawing.Bitmap(32, 32);
        using (var g = Drawing.Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Drawing.Color.Transparent);
            using var brush = new Drawing.SolidBrush(Drawing.Color.FromArgb(255, 79, 225, 255));
            g.FillEllipse(brush, 3, 3, 26, 26);
        }

        return Drawing.Icon.FromHandle(bitmap.GetHicon());
    }
}










