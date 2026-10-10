# Project Brief: AION 2 Space Rift Overlay

## Core Requirement
A standalone Windows desktop application — a small, attractive floating countdown
overlay for AION 2 "Space Rift" openings in Sofia local time.

## Hard Constraints
- NO game interaction: no injection, no memory reading, no game file modification,
  no automation, no game process interaction. Pure standalone WPF app.
- Windows x64 only; WPF + XAML; .NET 8 (net8.0-windows).
- Settings persist to %LOCALAPPDATA%\Aion2RiftOverlay\settings.json (never beside the exe).
- The published single-file EXE must be fully self-contained (no .NET Desktop Runtime
  needed by the recipient) INCLUDING the embedded voice-alert sound files.

## Schedule (user-provided, Sofia = "FLE Standard Time" Windows tz)
- Summer (EEST, UTC+3): 00:00 03:00 06:00 09:00 12:00 15:00 18:00 21:00
- Winter (EET, UTC+2):  02:00 05:00 08:00 11:00 14:00 17:00 20:00 23:00
- DST rules must come from the OS time-zone database, never manual month math.

## Delivery
- publish.ps1 → self-contained single-file win-x64 exe in .\publish\
- xUnit test project; every behavior change must keep the suite green.
