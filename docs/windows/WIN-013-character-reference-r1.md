# WIN-013 — Character reference (R1)

Estado: **manifesto de referencia de ingeniería para `proof_male_athletic`; aceptación visual pendiente**. Fecha: 2026-09-26.

## Propósito

Antes de ampliar catálogo o UI de taller, el personaje de prueba debe componerse desde un **paquete de referencia** reproducible: anclajes de rig, presets de cámara (cuerpo / cara) y captura **golden** con receta fija.

Esto alinea el runtime con [avatar-contract.json](../../data/art/avatar-contract.json) y evita números mágicos dispersos en código.

## Artefactos

| Archivo | Rol |
| --- | --- |
| [`ReferenceManifest.json`](../../app/Assets/SoloGym/Resources/AvatarReference/proof_male_athletic/ReferenceManifest.json) | Sockets, `camera_presets`, `stage_anchor`, `hit_regions`, receta golden |
| `AvatarReference/proof_male_athletic_front_golden.png` | Composición idle frontal sin equipo (generada por revisión Linux) |
| [`CharacterReference.cs`](../../app/Assets/SoloGym/Scripts/Avatar/CharacterReference.cs) | Carga manifest por `fit_family_id` |
| [`AvatarViewHost.cs`](../../app/Assets/SoloGym/Scripts/Avatar/AvatarViewHost.cs) | Un solo `LayeredAvatar`; `ApplyRecipe` in-place; preset desde manifest |
| [`AvatarRigLayout.cs`](../../app/Assets/SoloGym/Scripts/Avatar/AvatarRigLayout.cs) | Registro del rig proof: joints, rects de pantalla, orden de dibujo (hasta migrar a `rig_layout` en manifest) |

## Golden PNG (especificación)

- **Vista:** frontal proof, `action=idle`, `previewTime=0.55`, sin torso/guantes.
- **Receta:** la del bloque `golden_capture.recipe` en el manifest.
- **Viewport:** 512×640 (centro del stage del estudio).
- **Hash:** se registra en `expected_sha256` tras la primera captura aprobada.

Verificación: `-sologym-review -sologym-window character -sologym-avatar-reference-check` (Linux) escribe `artifacts/visual/AvatarReference/reference-check.json`. Tras retocar el rig, el SHA puede no coincidir hasta actualizar `expected_sha256` con aprobación visual.

## Puerta de aceptación

1. Smoke de adjuntos + `ValidateRecipe` en receta golden.
2. Diff/captura de referencia registrada (R2).
3. Aprobación humana en `design/approval-ledger.json` (`character_reference` gate).

Hasta entonces: no ampliar biblioteca de partes ni variantes de fit en catálogo.

## Estudio G3

Tras R1, la UI pasa a [`CharacterStudioScreen`](../../app/Assets/SoloGym/Scripts/CharacterStudioScreen.cs) (pantalla completa CAS). Ver [WIN-013-character-studio-g3.md](WIN-013-character-studio-g3.md).
