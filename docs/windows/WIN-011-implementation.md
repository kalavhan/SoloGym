# WIN-011 — Available equipment implementation

Status: **native UI implemented; Linux verification and user acceptance pending**. Date: 2026-09-26.

G0 flow and step-1 targets: [WIN-011-available-equipment-g0.md](WIN-011-available-equipment-g0.md). Steps 2–3 use the shared portal frame and live text.

## Scope

`EquipmentState.cs` holds in-memory `environment` and `equipment[]` aligned with [training-profile.schema.json](../../data/schemas/training-profile.schema.json). Catalog and per-environment visible ids load from `Resources/Equipment/Catalog.json` (mirrors [data/windows/WIN-011-available-equipment.json](../../data/windows/WIN-011-available-equipment.json)).

`EquipmentScreen.cs` composes three steps: environment rows, scrollable equipment checklist with bodyweight-only, review with change links.

Equipment step rebuilds preserve scroll offset when toggling rows. When the equipment list overflows its viewport, `SystemUI.AttachScrollAffordance` adds portal-style hints: a slim `Track`/`Fill` rail on the right, pulsing `SystemIcon` chevrons just above and below the masked list (not over rows), and tap-to-nudge by one row. **Solo peso corporal** is the first row inside the scroll content. Review step lays out environment, equipment summary, and change links using measured wrapped text so long selections do not overlap **Cambiar equipo**.

## Navigation

- Goals review checkpoint `REVIEW:WIN-011` opens this window via `OnboardingScreen.OpenEquipment`.
- Continue emits `REVIEW:WIN-012` and shows schedule notice (WIN-012 not implemented).
- Outdoor: empty derived list; user must confirm bodyweight only.
- Non-review mode blocks final checkpoint with `training_setup_pending`.

## Verification

`EquipmentStateChecks.Run()` covers production gate, home/gym selection, bodyweight path, outdoor rule, and checkpoint id.

```bash
app/Builds/Linux/SoloGym.x86_64 -screen-fullscreen 0 -screen-width 853 -screen-height 1844 \
  -sologym-review -sologym-window equipment -sologym-locale es \
  -sologym-capture "$PWD/artifacts/visual/WIN-011/equipment-es-final.png" -sologym-smoke
```

| Evidence | Path |
| --- | --- |
| Environment step ES | `artifacts/visual/WIN-011/equipment-es-final.png` |
| Smoke | `artifacts/visual/WIN-011/equipment-es-final.smoke.json` (`passed: true`) |

## Out of scope

Load ranges, named equipment profiles, Firestore, plan generation, APK.
