# Progress: AION 2 Space Rift Overlay

## DONE (verified by build/tests/runtime)
- [x] Project scaffold: sln + src (WPF net8.0-windows) + tests (xUnit)
- [x] RiftScheduleService: Sofia tz resolution ("FLE Standard Time" → "Europe/Sofia"
      → UTC+2 fallback), DST-aware schedule, next-opening policy (strictly later),
      injectable clock, no-drift recompute
- [x] DST edge cases: spring-forward skipped hour, fall-back day, noon-policy tests
- [x] Frameless rounded overlay UI: header "Aion2 x Sxystar", SPACE RIFT title,
      HH:MM:SS countdown (Consolas + glow), NEXT OPENING (Sofia HH:mm), status dot
      + EEST/EET label, tooltips
- [x] Dragging (not from interactive controls), lock-position, always-on-top
- [x] Opacity 20–100% slider (default 85%) on backdrop only; text stays opaque
- [x] Settings panel (toggle button), reset position, reliable close
- [x] Tray icon (Show overlay / Exit), minimize-to-tray option
- [x] Position persistence + work-area clamping + display-change re-clamp
- [x] Settings JSON persistence (atomic, malformed-safe) in %LOCALAPPDATA%
- [x] Voice alerts: 10-min warning + opening sound; embedded MP3s auto-extracted;
      settings toggle; state machine (once per opening, sleep/resume safe)
- [x] Portable .NET SDK 8.0.425 in .dotnet\ (no system-wide install)
- [x] publish.ps1 one-command publish → self-contained single-file win-x64 exe
- [x] README.md (setup, usage, schedule/DST policy, publish, distribution, troubleshooting)

## Test Results
- Latest run: 38/38 PASSED (build exit 0, publish exit 0)

## VERIFIED AT RUNTIME
- Published exe launches and runs (process alive); clean-PC extraction of embedded
  sounds verified.

## NOT DONE / OPEN
- [ ] In-game visibility over AION 2 NOT verified (user must test in Borderless Windowed)
- [ ] No volume control for alerts (possible future ask)
- [ ] No alert test button in settings
- [ ] exe is unsigned (SmartScreen warning on first run on other PCs)
