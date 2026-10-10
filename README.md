# AION 2 Space Rift Overlay

A small, dark, translucent floating countdown overlay for **AION 2** "Space Rift"
openings in Sofia time. Standalone WPF desktop app - it does **not** inject into
the game, read game memory, modify game files, or automate gameplay.

```
+------------------------------+
| AION II            . EEST  S _|
| SPACE RIFT                   |
|         02:14:38             |
| NEXT OPENING           15:00 |
+------------------------------+
```

- Header: `AION II`, schedule status (EEST/EET), settings button, minimize button
- Large cyan countdown in `HH:MM:SS`, updated once per second
- `NEXT OPENING` - next opening in local Sofia time (`HH:mm`)
- Click the settings button (S) to open the settings panel
- Dark translucent backdrop with rounded corners; text stays fully opaque

## Requirements

- Windows 10/11 x64
- To **build**: .NET 8 SDK (this workspace keeps a portable SDK in `.dotnet\`)
- To **run a published exe**: nothing else - it is self-contained
- To **run from source** without publishing: .NET 8 Desktop Runtime

## Time zone & schedule

Openings are defined in local Sofia wall-clock time
(time zone `FLE Standard Time` on Windows / `Europe/Sofia` IANA; the app falls
back to a fixed UTC+2 zone only if both lookups fail):

- **Summer (EEST, UTC+3):** 00:00, 03:00, 06:00, 09:00, 12:00, 15:00, 18:00, 21:00
- **Winter (EET, UTC+2):** 02:00, 05:00, 08:00, 11:00, 14:00, 17:00, 20:00, 23:00

DST rules come from the Windows time-zone database - the app never hardcodes
offsets per month. Policies (implemented in `RiftScheduleService` and covered by
unit tests):

1. The schedule for a given local date is chosen by the DST state of that date
   at local 12:00 noon (a time never inside a transition).
2. A scheduled local time skipped by spring-forward (e.g. 03:00 on the last
   Sunday of March) produces **no opening** that day.
3. A time repeated by fall-back is interpreted at its second (standard-time)
   occurrence; in practice no scheduled time falls inside Bulgaria's repeated hour.
4. An opening exactly at the current instant counts as the **current** opening;
   the overlay shows the next strictly later opening (so at exactly 15:00:00 the
   countdown jumps to the 18:00 slot).
5. The remaining time is recomputed from the current clock every second - there
   is no decrementing counter, so clock changes and sleep/resume self-correct.

Settings are stored in `%LOCALAPPDATA%\Aion2RiftOverlay\settings.json`
(window position, opacity, always-on-top, lock position, minimize-to-tray,
settings-panel state). Malformed files fall back to safe defaults.

## Build

```powershell
# from the workspace root (i:\Aion2RiftOverlay)
.dotnet\dotnet.exe build Aion2RiftOverlay.sln
.dotnet\dotnet.exe test tests\Aion2RiftOverlay.Tests\Aion2RiftOverlay.Tests.csproj
```

If you have a system-wide .NET 8 SDK, plain `dotnet build` / `dotnet test` work too.

## Run from VS Code / command line

```powershell
.dotnet\dotnet.exe run --project src\Aion2RiftOverlay\Aion2RiftOverlay.csproj
```

Or open the folder in VS Code, open `MainWindow.xaml` / any `.cs` file, and
launch the command above in a terminal. Stop it with the settings panel's
**CLOSE APP** button or the tray icon's **Exit** item.

## Publish a standalone executable

One command:

```powershell
powershell -ExecutionPolicy Bypass -File i:\Aion2RiftOverlay\publish.ps1
```

or manually:

```powershell
i:\Aion2RiftOverlay\.dotnet\dotnet.exe publish i:\Aion2RiftOverlay\src\Aion2RiftOverlay\Aion2RiftOverlay.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none `
  -o i:\Aion2RiftOverlay\publish
```

Output: `i:\Aion2RiftOverlay\publish\Aion2RiftOverlay.exe` - a **self-contained,
single-file, Windows x64** executable (~70-100 MB) that bundles the .NET 8
runtime. Target machines need **no** .NET Desktop Runtime, VS Code, or Cline.

Distribution notes:

- Copy the single `.exe` from the `publish` folder to any **Windows 10 (build
  1903+) / Windows 11 x64** machine. WPF's native libraries are embedded in the
  exe (`IncludeNativeLibrariesForSelfExtract`), so no DLLs ship beside it.
- Settings are always written to the user's `%LOCALAPPDATA%`, never beside the exe.
- Self-contained publishing bundles the runtime but does **not** guarantee
  compatibility with every Windows version or configuration.
- The overlay's visibility **over the game** depends on AION 2's display mode
  (Borderless Windowed works best) and Windows composition behavior. Visibility
  in exclusive fullscreen cannot be guaranteed, and behavior in the actual game
  has not been tested - do not assume it until verified in game.

## Usage

- **Drag** the overlay anywhere (drag on empty background areas).
- **Settings (S button):**
  - *Opacity* slider: 20-100 %, default 85 %. Applies to the translucent
    backdrop; the countdown text is always fully opaque for readability.
  - *Always on top* - default on.
  - *Lock position* - disables dragging.
  - *Minimize to system tray* - the minimize button then hides the window to
    the tray; restore via tray double-click or the tray menu.
  - *RESET POSITION* - recenters the overlay on the current screen.
  - *CLOSE APP* - reliable exit.
- The window position is saved (debounced) and restored on the next launch;
  invalid saved positions are clamped into the visible work area, including
  after monitor/resolution changes.
- Tray icon: double-click shows the overlay; right-click offers *Show overlay*
  and *Exit*.

## Troubleshooting

- **Countdown wrong**: system time zone must contain "FLE Standard Time" (any
  recent Windows 10/11 does). If time-zone data is broken the app falls back to
  a fixed UTC+2 schedule without DST.
- **Overlay hidden behind the game**: enable *Always on top*, use Borderless
  Windowed mode. Exclusive fullscreen games may cover overlays entirely.
- **Window lost off-screen**: click tray icon, then use *RESET POSITION*, or
  delete `%LOCALAPPDATA%\Aion2RiftOverlay\settings.json`.
- **Windows SmartScreen warning** on first run of the published exe: unsigned
  build - click *More info > Run anyway*.

## Project structure

```
Aion2RiftOverlay.sln
src/Aion2RiftOverlay/            WPF app (net8.0-windows)
  App.xaml(.cs)                  application + global exception handler
  MainWindow.xaml(.cs)           frameless overlay window mechanics
  Models/AppSettings.cs          persisted settings model
  Services/RiftScheduleService.cs  schedule + next-opening calculation
  Services/SettingsService.cs    JSON persistence (atomic writes)
  ViewModels/MainViewModel.cs    display state, MVVM-style
tests/Aion2RiftOverlay.Tests/    xUnit tests (schedule + settings)
publish.ps1                      one-command release build
```

