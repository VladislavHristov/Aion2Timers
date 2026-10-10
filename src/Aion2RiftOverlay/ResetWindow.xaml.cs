using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Aion2RiftOverlay.Services;
using Aion2RiftOverlay.ViewModels;
using Forms = System.Windows.Forms;

namespace Aion2RiftOverlay;

/// <summary>
/// Small reset-countdown overlay window (Daily or Weekly). Shares the main
/// overlay's visual style, backdrop opacity, topmost and lock-position state;
/// saves its own position separately.
/// </summary>
public partial class ResetWindow : Window
{
    private const double EstimatedHeight = 78;

    private readonly ResetKind _kind;
    private readonly ResetViewModel _vm;
    private readonly MainViewModel _main;
    private readonly DispatcherTimer _timer;
    private readonly string _positionTag;
    private bool _positionRestored;

    public ResetWindow(ResetKind kind, MainViewModel main)
    {
        _kind = kind;
        _main = main;
        _vm = new ResetViewModel(kind);
        _positionTag = kind == ResetKind.Daily ? "Daily" : "Weekly";

        InitializeComponent();
        DataContext = _vm;
        Title = _vm.TitleText;
        Topmost = main.IsTopMost;
        ApplyBackdropOpacity();

        main.PropertyChanged += OnMainPropertyChanged;

        _timer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        _timer.Tick += (_, _) => _vm.Refresh();
        _timer.Start();

        Loaded += (_, _) => ClampIntoWorkArea();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        RestorePosition();
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer.Stop();
        _main.PropertyChanged -= OnMainPropertyChanged;
        _main.SaveTaggedWindowPosition(_positionTag, Left, Top);
        base.OnClosed(e);
    }

    /// <summary>Show or hide the overlay (window instance and position are kept).</summary>
    public void SetOverlayVisible(bool visible)
    {
        if (visible)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    /// <summary>Re-apply the shared backdrop opacity from the main view model.</summary>
    public void SyncBackdropOpacity() => ApplyBackdropOpacity();

    private void RestorePosition()
    {
        if (_main.TryGetTaggedPosition(_positionTag, out var x, out var y))
        {
            Left = x;
            Top = y;
        }
        else
        {
            // Default: top-right of the primary work area; weekly below daily.
            var (ax, ay, aw, _) = GetWorkAreaDip(Forms.Screen.PrimaryScreen!);
            Left = ax + Math.Max(0, aw - Width - 30);
            Top = ay + (_kind == ResetKind.Daily ? 40 : 120);
        }

        _positionRestored = true;
        ClampIntoWorkArea();
    }

    private void RootBorder_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_main.IsLocked || e.Handled || e.ClickCount != 1)
        {
            return;
        }

        try
        {
            DragMove();
            _main.SaveTaggedWindowPosition(_positionTag, Left, Top);
        }
        catch (InvalidOperationException)
        {
            // Mouse button was already released; harmless.
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        // Hide via the main settings toggle so the checkbox stays in sync.
        if (_kind == ResetKind.Daily)
        {
            _main.ShowDailyResetOverlay = false;
        }
        else
        {
            _main.ShowWeeklyResetOverlay = false;
        }
    }

    private void OnMainPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.OpacityPercent))
        {
            ApplyBackdropOpacity();
        }
        else if (e.PropertyName == nameof(MainViewModel.IsTopMost))
        {
            Topmost = _main.IsTopMost;
        }
    }

    private void ApplyBackdropOpacity()
    {
        var alpha = (byte)Math.Round(255 * Math.Clamp(_main.OpacityPercent, 20, 100) / 100.0);
        RootBorder.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromArgb(alpha, 0x0C, 0x14, 0x1E));
    }

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
        }
    }

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

        return (area.Left, area.Top, area.Width, area.Height);
    }
}

