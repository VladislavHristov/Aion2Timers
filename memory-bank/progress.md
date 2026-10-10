# Progress: AION 2 Space Rift Overlay

## DONE (verified by build/tests/runtime)
- [x] Full WPF overlay (see projectbrief/productContext) - schedule, DST, UI, opacity,
      drag/lock/tray, settings persistence, voice alerts with embedded MP3s
- [x] Sounds override feature: sounds\ folder next to exe (priority over embedded
      defaults), settings.json paths still highest priority
- [x] Embedded defaults re-extracted on every startup (stale-extraction bug fixed)
- [x] GitHub repo: https://github.com/VladislavHristov/Aion2Timers (pushed, main)

## Test Results
- Latest run: 43/43 PASSED (build exit 0, publish exit 0)

## VERIFIED AT RUNTIME
- Published exe launches and runs; publish\sounds override folder auto-created.

## NOT DONE / OPEN
- [ ] In-game visibility over AION 2 NOT verified (user must test in Borderless Windowed)
- [ ] No volume control for alerts (likely future ask)
- [ ] No alert test button in settings
- [ ] exe unsigned (SmartScreen on first run)
- [ ] No GitHub Actions release workflow (offered to user)
