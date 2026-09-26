# WIN-013 — Personalización del personaje / Character customization

Estado: **G0 propuesto; interfaz nativa v1 (pantalla única, avatar de prueba) pendiente de aceptación**. Fecha: 2026-09-26.

Sigue a **WIN-012** en la ruta de configuración inicial. La apariencia del personaje es **independiente** de altura, peso y medidas privadas (WIN-009). Las elecciones **no** otorgan ventaja de combate ni prescripciones de entrenamiento.

## Alcance v1 (esta iteración)

**Una pantalla** en el marco portal compartido:

1. Vista previa del personaje (composición **LayeredAvatar** de ingeniería; no arte de producción).
2. Tono de piel (2 opciones de prueba).
3. Peinado (2 opciones de prueba).
4. Vista previa de equipamiento de prueba (activar/desactivar top y guantes de prueba).
5. **Continuar** → fin de la configuración inicial en revisión (`REVIEW:SETUP_COMPLETE`); **Ahora no** descarta borrador.

Pasos futuros del glosario (presentación femenina, complexión/músculo, matriz facial completa, taller con plataforma giratoria, guardado en cuenta) quedan **documentados pero fuera de alcance** hasta aprobar G0/G1 de arte.

## Datos

| Campo | Tratamiento |
| --- | --- |
| `AvatarAppearance` | IDs de prueba en memoria (`AvatarProof`); validación `IsSupportedProof`. |
| Persistencia | Solo borrador en memoria en modo revisión. |
| `depends_on` | WIN-009 (perfil privado completado en cadena). |
| `next` | `home_setup_complete` (Home / hub de entrenamiento; sin guardar cuenta). |

## Texto EN/ES (pantalla única)

| Español | English |
| --- | --- |
| TU PERSONAJE | YOUR CHARACTER |
| Elige cómo se ve tu personaje en el juego | Choose how your character looks in the game |
| Tono de piel | Skin tone |
| Cálido | Warm |
| Profundo | Deep |
| Peinado | Hairstyle |
| Espinoso | Spiky |
| Peinado lateral | Swept |
| Vista previa de equipo | Gear preview |
| Mostrar equipo de prueba | Show preview gear |
| Altura y peso son datos privados de entrenamiento. No definen tu apariencia. | Height and weight are private training data. They do not define your appearance. |
| CONTINUAR | CONTINUE |
| Ahora no | Not now |

## Dirección visual

Reutilizar **`Art/PortalBackground-v1`**, logotipo SoloGym y panel de vidrio cian como WIN-010–012. Zona central: pedestal/vista previa con silueta atlética de prueba (no el recorte estático de Home). Controles en filas de selección única como WIN-010. Renders G0 EN/ES: composición 853×1844 con panel, preview y filas; el runtime usa el atlas **AvatarProof** (calidad de ingeniería).

## Referencias retenidas

Manifest: `design/reference-manifests/WIN-013-character-customization-v1.json`. Propuestas: `design/renders/WIN-013-character-customization-{es,en}-proposal-v1.png`.

Ver [WIN-013-implementation.md](WIN-013-implementation.md) tras verificación nativa.
