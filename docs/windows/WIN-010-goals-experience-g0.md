# WIN-010 — Objetivos y experiencia

Estado: **G0 aprobado para implementación (flujo, datos y copia EN/ES; pasos 2–3 solo marco compartido + texto vivo)**. Fecha: 2026-09-26. La implementación nativa (G6) no sustituye el APK de revisión existente.

Esta ventana sigue al perfil privado para la ruta de acceso habilitada. Se apoya en los identificadores y límites del [esquema de perfil](../../data/schemas/training-profile.schema.json), las [reglas existentes](../../data/training/rules.json) y la [especificación del generador](../training/generation-spec.md). No introduce una nueva clasificación médica ni nuevas prescripciones de ejercicio.

## Flujo en tres pasos

1. **Tu ruta.** Elegir un enfoque. Para adultos: condición general, fuerza, desarrollo muscular, resistencia o movilidad. La elección empieza vacía; el render usa una selección ficticia de condición general. No hay objetivos de peso, calorías, estética ni recompensas por escoger una ruta más exigente.
2. **Tu punto de partida.** Describir la experiencia con dos opciones: “Estoy empezando o retomando” y “Entreno con regularidad y conozco los movimientos básicos”. Se corresponden con `beginner` e `intermediate`. “No estoy seguro” propone comenzar con la base y pide confirmar esa opción; no concede un nivel de forma automática. La apariencia del avatar no responde esta pregunta. Una rutina previa no certifica técnica ni determina por sí sola la dificultad.
3. **Revisar y continuar.** Mostrar enfoque y experiencia elegidos con enlaces para cambiarlos; explicar que todavía se necesitan equipo y disponibilidad antes de preparar las sesiones. Continuar lleva a WIN-011. Volver o cambiar idioma conserva el borrador, y salir descarta lo no guardado. Cambiar de objetivo más adelante no borra progreso.

Los tres pasos reutilizan el portal y el marco. Se conservan renders completos ES/EN del primer paso, sin generar una imagen por opción o por estado.

## Datos y reglas existentes

| Entrada | Valores o tratamiento |
| --- | --- |
| `goal` | `general_fitness`, `strength`, `muscle_growth`, `endurance`, `mobility`. |
| `experience` | `beginner` o `intermediate`; elección explícita, no inferida de medidas o avatar. |
| Edad/grupo | Procede del flujo privado de acceso, sin volver a solicitar fecha de nacimiento. |
| Preparación | Procede de WIN-009 y se vuelve a consultar al entrenar; esta ventana no transforma una pausa en disponibilidad. |
| Estado del borrador | Local y no guardado hasta que exista el servicio privado correspondiente. No se inventa recibo remoto. |

Para adolescentes, el primer alcance presenta **condición general** y **movilidad**, evitando ofrecer una especialización que las reglas actuales después sustituirían. El generador existente aplica una base general cuando recibe fuerza, desarrollo muscular o resistencia para menores de 18; una selección específica de movilidad conserva esa finalidad. Estos son límites del borrador de producto, no una afirmación de que otras actividades sean inadecuadas por edad.

La ruta adolescente conserva las condiciones de supervisión de fuerza del catálogo y las políticas de acceso aplicables. Elegir experiencia intermedia no habilita Hard para adolescentes. En adultos tampoco lo habilita automáticamente: las reglas existentes consideran experiencia, preparación y el resto del perfil. No se ofrecen metas de pérdida de peso a adolescentes ni se conecta esta selección con ayuno.

La [investigación ya registrada](../research/2026-09-25-fitness-game-research.md) fundamenta usar contenido estructurado, experiencia, objetivos, equipo, tiempo y preparación como entradas. Las dosis y clasificaciones concretas del catálogo siguen siendo contenido de producto pendiente de revisión profesional y juvenil; esta UI no resuelve esa revisión. No se cambia el algoritmo ni se prescribe una sesión desde esta propuesta.

## Dirección visual y texto

Mismo portal índigo/violeta, logotipo SoloGym, serif plateada y vidrio cian angular. Panel amplio con cinco opciones verticales de selección única; la primera seleccionada en el fixture adulto. Se conserva el arte aceptado de WIN-009 y su distribución principal. Sin personaje, equipamiento nuevo, gráficos corporales, barras de XP ni tarjetas blancas.

| Español | English |
| --- | --- |
| TU RUTA | YOUR PATH |
| Elige tu enfoque de entrenamiento | Choose your training focus |
| Condición general | General fitness |
| Fuerza | Strength |
| Desarrollo muscular | Muscle growth |
| Resistencia | Endurance |
| Movilidad | Mobility |
| Podrás cambiarlo cuando quieras. | You can change this at any time. |
| CONTINUAR | CONTINUE |

### Paso 2 — Experiencia (texto vivo; sin render retenido adicional)

| Español | English |
| --- | --- |
| TU PUNTO DE PARTIDA | YOUR STARTING POINT |
| Cuéntanos tu experiencia con el entrenamiento | Tell us about your training experience |
| Estoy empezando o retomando | I'm starting or returning |
| Entreno con regularidad y conozco los movimientos básicos | I train regularly and know the basic movements |
| No estoy seguro/a | I'm not sure |

**Diálogo “No estoy seguro/a”** (confirmación obligatoria; no se infiere nivel):

| Español | English |
| --- | --- |
| ¿Empezar con la base? | Start with the basics? |
| Podemos comenzar con un punto de partida para principiantes. Podrás ajustarlo más adelante. Esto no evalúa tu capacidad. | We can begin with a beginner starting point. You can adjust this later. This does not assess your ability. |
| USAR BASE PARA PRINCIPIANTES | USE BEGINNER STARTING POINT |
| VOLVER | GO BACK |

### Paso 3 — Revisar (texto vivo; sin render retenido adicional)

| Español | English |
| --- | --- |
| REVISAR Y CONTINUAR | REVIEW AND CONTINUE |
| Enfoque | Focus |
| Experiencia | Experience |
| Cambiar enfoque | Change focus |
| Cambiar experiencia | Change experience |
| Todavía elegirás equipo y horario antes de preparar las sesiones. En esta vista previa no se guarda nada en tu cuenta. | You'll still choose equipment and schedule before sessions are prepared. Nothing is saved to your account in this preview. |
| Cambiar el enfoque más adelante no borra tu progreso. | Changing your focus later won't erase your progress. |
| CONTINUAR | CONTINUE |
| Volver | Back |
| Ahora no | Not now |

### Ruta adolescente (dos enfoques visibles)

| Español | English |
| --- | --- |
| Para tu edad, estos enfoques están disponibles. | For your age, these training focuses are available. |

Solo se muestran **condición general** y **movilidad** (`teen_visible_goals`). Fuerza, desarrollo muscular y resistencia no aparecen en la lista.

### Entrada inicial vs fixture del render

En tiempo de ejecución, **goal** y **experience** empiezan vacíos; el usuario debe elegir explícitamente. Los PNG de propuesta del paso 1 muestran **condición general** seleccionada solo como referencia visual del fixture ficticio.

El detalle y la confirmación de los pasos 2 y 3 se presentan mediante texto vivo sobre el mismo portal (`Art/PortalBackground-v1`, `SystemUI.PortalPage` con panel 718×965, coherente con WIN-009 migrado). La primera selección no prepara todavía un plan: WIN-011 y WIN-012 completan equipo y tiempo, y los requisitos de acceso, datos privados y contenido permanecen activos.

## Referencias retenidas

Generadas con la herramienta integrada **ImageGen** tras construir WIN-009 y obtener sus primeras comprobaciones nativas. La composición ES parte del objetivo aprobado de WIN-009; EN localiza el nuevo render ES. Ambas miden **853 × 1844**. El [manifiesto v1](../../design/reference-manifests/WIN-010-goals-experience-v1.json) conserva archivos, dimensiones, hashes y fixture. Los [prompts ES](../../design/prompts/WIN-010-goals-experience-es-v1.txt) y [EN](../../design/prompts/WIN-010-goals-experience-en-v1.txt) guardan exactamente lo enviado a la herramienta.

![Objetivos y experiencia — Español](../../design/renders/WIN-010-goals-experience-es-proposal-v1.png)

![Goals and experience — English](../../design/renders/WIN-010-goals-experience-en-proposal-v1.png)

Se inspeccionaron ambas ventanas completas: cinco opciones legibles, solo la primera seleccionada en el fixture, copia ES/EN correcta, portal y marcos conservados, sin solapamientos ni recortes visibles. Son renders propuestos, no capturas de una UI implementada. Sus dimensiones y hashes se preservarán como referencia si el usuario los acepta.

## Alineación visual paso 1 (G0)

Los renders retenidos del paso 1 conservan portal, logotipo, panel de vidrio y filas de selección alineados con WIN-009. La implementación nativa reutiliza **`Art/PortalBackground-v1`** y la geometría **`PortalPage(718, 965)`**; no se cargan los PNG de propuesta como texturas de controles. Las diferencias esperadas son tipografía nativa, ajuste de texto y estados vacío/seleccionado en vivo frente al fixture con primera fila marcada.

**G0 registrado:** flujo de tres pasos, copia EN/ES completa, pasos 2–3 sin imágenes retenidas adicionales, comportamiento adolescente e incertidumbre según `data/windows/WIN-010-goals-experience.json`. G1 reutiliza el fondo de portal aprobado (N/A personaje nuevo). El equipo, la agenda y el avatar siguen fuera de esta ventana.
