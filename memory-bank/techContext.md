# Technology Context: AION 2 Space Rift Overlay

## Stack
- C# / .NET 8 (net8.0-windows), WPF + XAML, Windows x64, Windows 10 build 19045.
- WinForms interop (UseWindowsForms=true) only for NotifyIcon tray + Screen work areas.
- System.Text.Json for settings.
- xUnit (Microsoft.NET.Test.Sdk 17.11.1, xunit 2.9.2, xunit.runner.visualstudio 2.8.2).
- Target machine needs NO .NET Desktop Runtime (self-contained publish).

## Build Environment
- Portable .NET SDK 8.0.425 in .dotnet\ (no admin needed; installed via
  dotnet-install.ps1 because no system SDK exists).
- All commands: i:\Aion2RiftOverlay\.dotnet\dotnet.exe ...

## Commands
- Build:  .dotnet\dotnet.exe build Aion2RiftOverlay.sln
- Test:   .dotnet\dotnet.exe test tests\Aion2RiftOverlay.Tests\Aion2RiftOverlay.Tests.csproj
- Run:    .dotnet\dotnet.exe run --project src\Aion2RiftOverlay\Aion2RiftOverlay.csproj
- Publish: powershell -ExecutionPolicy Bypass -File publish.ps1
  (= -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
   -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -o publish)
- Output EXE: i:\Aion2RiftOverlay\publish\Aion2RiftOverlay.exe (~154 MB)

## User Data
- %LOCALAPPDATA%\Aion2RiftOverlay\settings.json  (opacity, topmost, lock, tray, panel
  state, window position, voiceAlertsEnabled, sound file paths)
- %LOCALAPPDATA%\Aion2RiftOverlay\sounds\10minutes.mp3 / OpenNow.mp3 (auto-extracted)

## Known Source of Truth
- Schedule times are user-provided; if the game changes rift times, update
  SummerHours/WinterHours in RiftScheduleService.cs.
