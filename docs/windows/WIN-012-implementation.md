# WIN-012 — Schedule / session time implementation

Status: **native UI implemented (single setup screen); Linux verification and user acceptance pending**. Date: 2026-09-26.

G0 summary: [WIN-012-schedule-session-time-g0.md](WIN-012-schedule-session-time-g0.md).

## Scope

**Setup (single screen):** weekday toggles, inline **scroll wheels** for hours **0–12** and minutes **00–60** (labels to the right of each wheel), scrollable helper when copy overflows. Glass **`PortalPage(380, 1303)`** max (bottom **1683**); **[`PortalWindowFrame`](../../app/Assets/SoloGym/Scripts/UI/PortalWindowFrame.cs)** measures setup scroll content, sizes the viewport to content (cap **615px**), places the time/wheel band **24px** below scroll (`ScheduleScrollToTimeGap`), and keeps footer on **[`PortalFrameLayout`](../../app/Assets/SoloGym/Scripts/UI/PortalFrameLayout.cs)** (`PrimaryTop` **1545**, `SecondaryTop` **1645**). **`availability_block_minutes`** up to **12 h** in memory; **`session_minutes`** derived clamp for generator placeholder only.

**Review step removed:** **Continuar** on setup advances to WIN-013 (character notice in review chain); no **Revisa tu rutina** screen.

`ScheduleState.cs` / `ScheduleScreen.cs`; config in `Resources/Schedule/Config.json`.

## Navigation

- Equipment checkpoint `REVIEW:WIN-012` → `OpenSchedule`.
- Continue on setup → `REVIEW:WIN-013` notice (next window).

## Verification

```bash
app/Builds/Linux/SoloGym.x86_64 -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window schedule -sologym-locale es \
  -sologym-capture "$PWD/artifacts/visual/WIN-012/schedule-es-final.png" -sologym-smoke
```

Add `-sologym-schedule-view review` to prefill setup fields for captures (still one screen).

| Evidence | Path |
| --- | --- |
| Setup ES | `artifacts/visual/WIN-012/schedule-es-final.png` |
| Smoke | `artifacts/visual/WIN-012/schedule-es-final.smoke.json` (`passed: true`) |

## Out of scope

Firestore, notifications, schema extension for availability window, APK.
