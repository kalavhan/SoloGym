# WIN-012 — Horario / Duración de sesión

Estado: **G0 actualizado; interfaz nativa de pantalla única implementada; aceptación del usuario pendiente**. Fecha: 2026-09-26.

## Flujo

1. **Configurar horario (pantalla única).** Días de entrenamiento (2–5), ruedas de **horas (0–12)** y **minutos (0–60)** para tiempo disponible, texto de ayuda desplazable. **Continuar** → WIN-013 (sin pantalla intermedia “Revisa tu rutina”).

## Datos

| Campo | Uso |
| --- | --- |
| `availability_block_minutes` | Lo que elige el usuario (hasta 4 h). |
| `session_minutes` (derivado) | `clamp(disponibilidad, 15, 60)` solo como marcador del generador hasta ampliar el esquema. |
| `training_day_indices` | Borrador en memoria. |

No implica una sesión boss continua de 4 h; las sesiones pueden pausarse y retomarse dentro de la ventana. Notificaciones de entrenamiento incompleto: trabajo futuro.

Renders G0 paso 1 (solo días) **supersedidos** por la pantalla combinada; portal `PortalPage` ~620×1140.

Ver [WIN-012-implementation.md](WIN-012-implementation.md) para verificación.
