# System Patterns: AION 2 Space Rift Overlay

## Architecture (MVVM-lite)
- MainWindow.xaml(.cs): frameless overlay mechanics only — DragMove (buttons/sliders/
  checkboxes swallow mouse-down, so dragging never starts from them), position
  restore + work-area clamping via System.Windows.Forms.Screen with DPI transform
  (TransformFromDevice/TransformToDevice), SystemEvents.DisplaySettingsChanged re-clamp,
  NotifyIcon tray (System.Drawing icon extracted from exe), DispatcherTimer 1s →
  _vm.Refresh().
- MainViewModel: display state + alert state machine; events RequestClose /
  RequestResetPosition / AlertRequested(kind); settings saved on every change.
- RiftScheduleService (pure logic, injectable clock Func<DateTimeOffset>):
  - Resolves tz: "FLE Standard Time" → "Europe/Sofia" → custom fixed UTC+2 fallback.
  - Schedule per local date chosen by DST state at local NOON (never inside transition).
  - Invalid (spring-forward skipped) local times → no opening.
  - Ambiguous (fall-back) times → second/standard occurrence (ConvertTimeToUtc default).
  - Policy: opening at exact current instant = CURRENT; GetNextOpening returns the
    next STRICTLY LATER opening.
  - Remaining time always recomputed from current instant (no decrementing counter;
    sleep/resume and clock changes self-correct).
- SoundAlertService: WPF MediaPlayer fire-and-forget; never throws.
- SettingsService: JSON (camelCase, indented), atomic write (tmp + Move overwrite),
  malformed → safe defaults; AppSettings.Sanitize() clamps values.
- ResetScheduleService: daily 07:00 UTC, weekly Wednesday 07:00 UTC; instants UTC,
  displayed via a settable DisplayTimeZone (sofia by default). Same "strictly later"
  policy as the rift schedule.
- TimeZoneService: user-selectable DISPLAY zone. Schedule INSTANTS always Sofia/
  UTC-based; only presentation converts. Resolve(id) falls back to Sofia on
  empty/unknown id. AppSettings.DisplayTimeZoneId persists the choice.
- MainViewModel.SelectedTimeZone (ComboBox bound to TimeZoneOptions): sets
  DisplayTimeZone, saves, Refresh(); ResetWindow listens for PropertyChanged and
  forwards to ResetViewModel.SetDisplayTimeZone → instant sync of mini overlays.

## Alert State Machine (in MainViewModel.EvaluateAlerts)
- _armedOpening = opening being counted down; _warnedFor = opening that got its warning.
- now >= armed → fire "opening", clear both.
- armed unset or changed → re-arm to next opening.
- remaining <= 10 min && not warned → fire "ten-minute" once.
- Disabled toggle clears all state. Fresh start after an opening → no stale alerts.

## Key Decisions
- Backdrop alpha instead of Window.Opacity (text readability at 20%).
- WinForms interop enabled ONLY for NotifyIcon + Screen.
- Voice MP3s embedded as resources (Aion2RiftOverlay.Sounds.*), extracted to
  %LOCALAPPDATA%\Aion2RiftOverlay\sounds on startup if missing → single exe is
  fully self-contained.

## Environment Quirks (this workspace)
- No system-wide .NET SDK: portable SDK lives in .dotnet\ (dotnet.exe there).
- Terminal output capture unreliable: run long build/test/publish detached via
  Start-Process powershell -File <script>, redirect output to a log file, poll
  the file (avoid Start-Sleep >3s in foreground commands — they get killed).
- The editor tool's old_text replacement is unreliable here; prefer delete+recreate
  via editor, or PowerShell single-line .Replace (multi-line replaces need regex
  with \s* due to line-ending differences).

## USER RULES (must always be followed)
- **Commit, push and memory-bank updates ONLY when the user explicitly asks for it.**
  Never commit or push spontaneously, even after large feature work. Code + build +
  tests may proceed freely; git actions and memory-bank writes are opt-in.
