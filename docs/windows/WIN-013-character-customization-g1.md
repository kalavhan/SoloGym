# WIN-013 — Character workshop UX (G1)

Estado: **G1 propuesto; runtime workshop implementado (catálogo + prueba de ingeniería)**. Fecha: 2026-09-26.

G0: [WIN-013-character-customization-g0.md](WIN-013-character-customization-g0.md). Implementación: [WIN-013-implementation.md](WIN-013-implementation.md).

## Cambio respecto a G0 / v1 portal

Una sola pantalla con **scroll de taller**, no chips de texto tipo formulario:

| Zona | Comportamiento |
| --- | --- |
| Vista previa (fija) | Cuerpo completo con `LayeredAvatar` |
| **Piel** | Fila de **cajas de color** (8 tonos vía tint del shader; sin sprites nuevos) |
| **Cara** | Recorte **primer plano** + filas de miniaturas: peinado, ojos, boca |
| **Cuerpo** | Filas de **fit family** (complexión/tamaño); solo `proof_male_athletic` habilitado; resto “Próximamente” |
| **Equipo** | Una fila por ranura (`torso`, `hands`, `head`, `legs`, `feet`): Ninguno + ítems de prueba donde existan |

Catálogo: `app/Assets/SoloGym/Resources/AvatarCustomization/Catalog.json`. Validación compartida: `AvatarCustomizationCatalog.ValidateRecipe` + `AvatarAppearance.IsSupportedProof`.

## Contrato torre / juego

La receta `AvatarAppearance` es la cara de onboarding del contrato ([avatar-contract.json](../../data/art/avatar-contract.json)). El juego de torre resolverá los mismos IDs cuando existan clips y fits exportados.

## Fuera de alcance G1

Arte de taller G2+, presentación femenina, ocho direcciones, persistencia en cuenta, sustituir retrato estático de Home, APK.

## Renders G1

Manifest: `design/reference-manifests/WIN-013-character-customization-g1-v1.json`. Propuestas: `design/renders/WIN-013-character-customization-{es,en}-proposal-g1-v1.png`.
