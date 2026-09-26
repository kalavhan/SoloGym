# WIN-009 — Implementación del perfil físico privado

Estado: **interfaz nativa implementada y verificada en Linux; aceptación del usuario pendiente**. Fecha: 2026-09-25.

El usuario aprobó los [renders v1 EN/ES y el flujo de tres pasos](WIN-009-private-fitness-profile-g0.md) con “si perfecto”. Se conserva la instrucción de construir la ventana completa, realizar después una revisión visual enfocada y preparar la siguiente propuesta. **No se genera ni se sustituye el APK.**

## Alcance aprobado

La ventana presenta el aviso de datos privados y la decisión específica que corresponda, después las medidas opcionales en unidades métricas o imperiales y finalmente el estado actual de preparación. La ruta real no recoge medidas mientras falten sus dependencias. La vista previa carga el fixture ficticio 170 cm / 70 kg después del aviso; borrar ambos permite continuar sin medidas. Cambiar unidades preserva el valor normalizado; borrar una medida la mantiene ausente. La altura imperial usa campos separados de pies y pulgadas, y el peso usa libras.

La respuesta de preparación no diagnostica ni certifica aptitud. Dolor, lesión, enfermedad o incertidumbre llevan a una pausa, sin presionar a completar un entrenamiento. Experiencia y objetivos siguen perteneciendo a WIN-010. Las medidas no clasifican el cuerpo, no modifican el avatar y no se publican en rankings, chat ni perfil público.

## Estado de datos y servicios

El alcance local usa un borrador privado en memoria y datos ficticios en la vista previa. Una decisión local no sustituye la política revisada, el documento aplicable ni un recibo autenticado del servidor. Las dependencias de país, edad, tutor cuando corresponda y acceso de WIN-006/007 siguen vigentes; la vista previa no las habilita en producción.

Firestore no está conectado por esta ventana. No se afirma guardado remoto, consentimiento legal completado ni que el generador use altura o peso: el contrato actual de entrenamiento no requiere estas medidas. Las condiciones de publicación del contenido de entrenamiento siguen registradas en la [especificación de generación](../training/generation-spec.md).

## Implementación y navegación

`PrivateProfileState.cs` mantiene el borrador privado en memoria. `PrivateProfileScreen.cs` construye los tres pasos y las salidas de pausa sobre el arte aprobado, con texto e inputs vivos. Desde la vista previa de WIN-007 se llega al aviso de WIN-009; Volver conserva el mismo borrador incluso entre ventanas. Ahora no/cerrar elimina el borrador. La sesión Firebase no se elimina.

Las unidades canónicas usan decimales en cm/kg; alternar unidades no reinterpreta el valor redondeado mostrado. Pies y pulgadas se editan por separado. Punto o coma pueden ser separadores decimales; no se aceptan exponentes, negativos ni formatos mixtos. Los límites amplios de entrada de 1–300 cm y 1–1000 kg son guardas técnicas, no clasificaciones médicas. Entradas inválidas se conservan para corregirlas y no desaparecen al cambiar unidades o volver.

El aviso de datos de salud es separado del consentimiento general. No se presenta una aceptación ficticia como válida: la ruta real queda detenida antes de recoger medidas mientras falten la política y el servicio de producción. Las respuestas listo/poca energía llegan solo a `REVIEW:WIN-010`; dolor, lesión, enfermedad o incertidumbre muestran la pausa. No se crean entrenamientos ni se certifica aptitud en esta ventana.

## Verificación enfocada

El reproductor Linux **0.4.0** compiló. Pasaron **12 grupos de comprobaciones del perfil** y **6 grupos de interacción**: campos imperiales, ida/vuelta de unidades, omitir medidas, preparación, pausa por dolor y checkpoint de revisión. También pasó la prueba entre ventanas que modifica medidas, cambia a imperial, vuelve dos veces y reentra conservando instancia, textos y valores. Esa ejecución conservó las 11 comprobaciones del controlador anterior y sus 8 grupos de interacción.

| Evidencia | Archivo |
| --- | --- |
| Medidas ES | [Captura](../../artifacts/visual/WIN-009/profile-es-final.png) · [Comparación](../../artifacts/visual/WIN-009/comparison-es.side-by-side.png) |
| Medidas EN | [Captura](../../artifacts/visual/WIN-009/profile-en-final.png) · [Comparación](../../artifacts/visual/WIN-009/comparison-en.side-by-side.png) |
| Unidades imperiales | [Captura EN](../../artifacts/visual/WIN-009/imperial-en-final.png) |
| Aviso y preparación | [Aviso ES](../../artifacts/visual/WIN-009/notice-es-final.png) · [Preparación ES](../../artifacts/visual/WIN-009/readiness-es-final.png) |
| Resultados | [Perfil](../../artifacts/visual/WIN-009/profile-es-final.smoke.json) · [Integración](../../artifacts/visual/WIN-009/onboarding-integration.smoke.json) · [Build y hashes](../../artifacts/visual/WIN-009/build-verification.json) |

Se revisaron pantallas completas, sin generar imágenes por control. La composición usa las referencias de cada idioma y conserva sus hashes. El texto nativo, su sombreado y las zonas reconstruidas presentan diferencias; el aviso de vista previa es visible en las capturas. Las métricas son descriptivas, sin declarar igualdad exacta de píxeles.

No se generó APK ni binario iOS; el APK 0.2.1 anterior mantiene su SHA-256. El ajuste con teclado usa el campo enfocado, pero la verificación de teclados Android/iOS y la aceptación visual del usuario siguen pendientes. No se han guardado medidas en Firestore ni en PlayerPrefs; solo se persiste el idioma.

## Siguiente ventana

Tras verificar WIN-009 se preparó **WIN-010 · Objetivos / Experiencia**, con selecciones compatibles con las reglas de entrenamiento existentes. Será una nueva propuesta con renders retenidos y requerirá aprobación antes de implementarla. El equipo físico y la agenda continúan en WIN-011 y WIN-012.
