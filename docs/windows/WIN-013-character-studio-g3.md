# WIN-013 — Character studio (G3)

Estado: **G3 implementado (pantalla completa CAS); referencia R1 pendiente de aceptación visual**. Fecha: 2026-09-26.

Referencia R1: [WIN-013-character-reference-r1.md](WIN-013-character-reference-r1.md). G1 histórico: [WIN-013-character-customization-g1.md](WIN-013-character-customization-g1.md).

## Experiencia

- **Escenario completo** 853×1844: fondo portal, personaje hero (~780px stage), sin scroll de formulario.
- **Un solo** [`AvatarViewHost`](../../app/Assets/SoloGym/Scripts/Avatar/AvatarViewHost.cs): receta in-place, presets desde [`ReferenceManifest.json`](../../app/Assets/SoloGym/Resources/AvatarReference/proof_male_athletic/ReferenceManifest.json). Rig proof en [`AvatarRigLayout.cs`](../../app/Assets/SoloGym/Scripts/Avatar/AvatarRigLayout.cs) (idle estable con `freeze`).
- **Dock** (Piel · Cara · Cuerpo · Equipo · Girar): categoría activa resaltada.
- **Hoja overlay** (vidrio): carrusel horizontal de la categoría activa (swatches, cara, fit, equipo por ranura).
- **Toque en silueta** (G3b): regiones `hit_regions` del manifest abren categoría.
- Footer: **Continuar** / **Ahora no** (tokens `PortalFrameLayout`).

## Comandos

```bash
# Estudio + smoke
app/Builds/Linux/SoloGym.x86_64 ... -sologym-review -sologym-window character \
  -sologym-locale es -sologym-capture "$PWD/artifacts/visual/WIN-013/studio-es-g3.png" -sologym-smoke

# Comprobación de referencia (golden sha pendiente hasta captura aprobada)
app/Builds/Linux/SoloGym.x86_64 ... -sologym-review -sologym-window character \
  -sologym-avatar-reference-check
```

Salida referencia: `artifacts/visual/AvatarReference/reference-check.json`.

## Renders G3

`design/renders/WIN-013-character-studio-{es,en}-proposal-g3-v1.png` · manifest `design/reference-manifests/WIN-013-character-studio-g3-v1.json`.
