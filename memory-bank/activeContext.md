# Active Context: Aion2Timers (AION 2 Space Rift Overlay)

## Current State (2026-10-10)
Application COMPLETE, built, tested (58/58), published, running, pushed to GitHub:
https://github.com/VladislavHristov/Aion2Timers (branch main).

## Recently Completed
1. Header text "Aion2 x Sxystar" on main overlay.
2. Voice alerts: 10-min warning + opening sound; MP3s embedded in exe, auto-extracted
   to %LOCALAPPDATA% on startup (always overwritten, so rebuilds update the voices).
3. Sound override priority: settings.json path > sounds\ folder NEXT TO THE EXE
   (auto-created, swap voices without recompiling) > embedded defaults.
4. Daily Reset + Weekly Reset overlay windows (user request):
   - ResetScheduleService: daily 07:00 UTC; weekly Wednesday 07:00 UTC; UTC-based
     (DST handled by display conversion only; shows 10:00 EEST / 09:00 EET).
   - One generic ResetWindow used twice (ResetKind.Daily/Weekly), same dark style,
     MINI size (132px wide, "Aion2" header text), weekday shown for weekly ("Wed 10:00",
     invariant culture so Bulgarian OS locale does not localize it).
   - Own saved positions (HasDaily/WeeklyWindowPosition), drag with main lock,
     opacity + topmost synced from main VM, close button unchecks the main toggle.
   - Checkboxes "Show Daily/Weekly Reset overlay" in main settings panel.
5. Compact sizes: main Space Rift overlay 300->250px wide (fonts/margins reduced,
   EstimatedHeight 158); reset windows mini (132px, EstimatedHeight 78).

## Standing User Rules
- Commit / push / update memory bank ONLY on explicit user request (as done here).
- User speaks Bulgarian; respond in Bulgarian, technical terms in English.

## Potential Next Steps
- Volume control / alert test button; GitHub Actions release workflow;
  in-game visibility verification (user must test); code signing.
