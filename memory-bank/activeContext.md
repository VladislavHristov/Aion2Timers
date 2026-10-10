# Active Context: AION 2 Space Rift Overlay

## Current State (2026-10-10)
Application COMPLETE, built, tested (43/43), published, running, and pushed to GitHub:
https://github.com/VladislavHristov/Aion2Timers (branch main; commits 2bcd2fa -> d8a93b4 -> 6e60045).
Local repo origin = that URL; git identity = VladislavHristov (noreply email); gh CLI auth used.

## Recently Completed
1. Header text "Aion2 x Sxystar" (user request).
2. Voice alerts: 10-min warning + opening sound; embedded MP3s; settings toggle.
3. Sounds override feature: priority settings.json > sounds\ folder NEXT TO THE EXE
   (auto-created; swap voices without recompiling) > embedded defaults.
4. Embedded defaults now RE-EXTRACTED (overwritten) on every startup, so a repo user
   who swaps Sounds\*.mp3 and rebuilds hears the new files (old stale-extraction bug fixed).
5. Repo pushed to GitHub; scratch files ignored via .gitignore (.git.txt, .gitpush.txt, ..work.txt).

## User Communication
- User speaks Bulgarian; respond in Bulgarian (short), technical terms in English.
- User wants future voice files managed through the sounds folder concept.

## Potential Next Steps (not requested yet)
- Volume setting / alert test button.
- GitHub Actions workflow to build/publish the exe as a Release on push.
- In-game visibility verification (user must test).
- Code signing (SmartScreen).
