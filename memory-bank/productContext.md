# Product Context: AION 2 Space Rift Overlay

## Why
The player (Sxystar) plays AION 2 in Borderless Windowed mode and wants a persistent,
readable countdown of when the next Space Rift opens (every 3 hours in Sofia time),
plus a voice warning before it happens.

## User Experience Goals
- Always-on-top, frameless, rounded, dark translucent gaming-style overlay (300px wide).
- Large cyan countdown HH:MM:SS; "NEXT OPENING" in Sofia time (HH:mm).
- Draggable anywhere; lock option; position remembered; never lost off-screen.
- Opacity 20–100% (default 85%) applied ONLY to the backdrop — countdown text stays
  fully opaque for readability (chosen over Window.Opacity deliberately).
- Minimize-to-tray optional; tray icon with Show/Exit; reliable close (CLOSE APP button
  and tray Exit).
- Voice alerts: 10minutes.mp3 at T-10:00, OpenNow.mp3 at T-0, toggle in settings panel.
- Personalized header: "Aion2 x Sxystar".

## Visibility Disclaimer
Overlay visibility over the game depends on the game's display mode (Borderless
Windowed works best) and Windows composition; exclusive fullscreen may cover it.
Not verified in the actual game yet.
