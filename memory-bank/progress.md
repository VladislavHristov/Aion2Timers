# Progress: Aion2Timers (AION 2 Space Rift Overlay)

## DONE (verified by build/tests/runtime)
- [x] Full WPF overlay: Sofia-time Space Rift schedule, DST-aware, frameless drag/lock,
      opacity on backdrop, tray, settings JSON persistence, voice alerts (embedded MP3s,
      sounds\ override folder next to exe, settings.json paths)
- [x] Daily Reset + Weekly Reset overlay windows (07:00 UTC daily; Wednesday 07:00 UTC
      weekly), mini style (132px, "Aion2" header), own positions, synced opacity/topmost,
      show/hide checkboxes in main settings panel
- [x] Main overlay compacted to 250px width
- [x] README: real screenshots (capture-screens.ps1, make-hero-shot.ps1 -> docs/
      screenshots/overlays.png hero + three individual shots), "Making your own .exe"
      section for repo visitors
- [x] Display TIME ZONE selector (ComboBox in settings; instants stay Sofia/UTC,
      only display converts; persisted in settings.json; reset overlays sync live;
      TimeZoneService + TimeZoneSelectionTests)
- [x] GitHub repo https://github.com/VladislavHristov/Aion2Timers (branch main)

## Test Results
- Latest run: 68/68 PASSED (build exit 0, publish exit 0)

## NOT DONE / OPEN
- [ ] In-game visibility over AION 2 NOT verified (user must test in Borderless Windowed)
- [ ] No volume control / alert test button
- [ ] exe unsigned (SmartScreen on first run)
- [ ] No GitHub Actions release workflow (offered)

