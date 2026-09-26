# WIN-006 + WIN-007 — Implementación de origen y privacidad

Estado: **UI nativa implementada y verificada en Linux; aceptación del usuario pendiente**. Fecha: 2026-09-25.

El usuario aprobó los cuatro renders v1 y la construcción conjunta con: “aprobado, no hay necesidad de generar el apk aun. implementa y terminanndo prepare la siguiente ventana”. Se aplica el flujo TDR acordado: construir las ventanas completas con arte compartido y texto vivo, después verificar las capturas completas y las interacciones relevantes. **No se genera ni sustituye el APK** en esta iteración.

Los [renders aprobados y el flujo](WIN-006-007-onboarding-proposal.md) conservan sus archivos y hashes. Esta aprobación define el objetivo visual; no constituye aceptación de píxeles nativos, un servicio legal desplegado ni la publicación de las políticas por país.

## Alcance de construcción

| Ventana | Comportamiento implementado |
| --- | --- |
| WIN-006 · Tu origen | Edad autodeclarada privada, país elegido explícitamente y subdivisión opcional; validación, selector localizado, borrador y continuación a privacidad. |
| WIN-007 · Tu privacidad, tus decisiones | Decisiones inicialmente vacías, lector de documentos versionados, estados de documentos o servicio no disponibles, volver sin perder el borrador y salida sin registrar aceptación. |
| Compartido | Inglés/español, navegación desde acceso y creación de cuenta, recuperación de errores, marco del portal y mapa de posiciones conservado. |

Las políticas regionales, los documentos finales y su servicio de guardado son dependencias explícitas. No se transforma una casilla local en autorización de adulto, consentimiento de salud ni un recibo del servidor. Si faltan estos recursos, la interfaz debe mostrar el estado pendiente y no inventar un guardado. El proyecto Firebase Authentication identifica una cuenta; eso no significa que Firestore ya guarde el onboarding.

## Evidencia de implementación

`OnboardingScreen.cs` construye las dos pantallas con texto vivo y controles Unity; `OnboardingState.cs` mantiene el borrador privado en memoria. `WelcomeScreen.cs` abre edad/región tras el checkpoint de acceso o creación de cuenta y muestra el lector de privacidad/términos desde sus enlaces. El menú de idioma incorpora una vista previa explícita para recorrer ambas ventanas.

El catálogo contiene países de los mercados previstos y subdivisiones de Estados Unidos, Canadá y México. Estar en ese catálogo no habilita un país: falta la política regional revisada. Para países sin subdivisiones cargadas, la selección regional sigue siendo opcional.

La edad acepta números enteros, valida el mínimo de 15 y no se infiere de la apariencia. Cambiar de país limpia la región; cambiar idioma conserva el borrador y respeta la detección automática. El lector muestra un estado honesto de documento no disponible. Volver conserva datos; salir descarta el borrador. Las casillas están vacías al entrar. La vista previa permite ensayar sus estados sin guardar ni emitir recibos; la ruta real permanece bloqueada por las dependencias de producción.

**Pendiente para producción:** políticas de elegibilidad, documentos legales finales y servicio autenticado de persistencia/recibos. No se ha conectado Firestore ni implementado un guardado remoto o su reintento. No se certifica consentimiento legal ni onboarding completado. Las rutas condicionales a registro/tutor/perfil siguen delimitadas por sus hitos.

## Verificación enfocada

El reproductor Linux **0.3.0** compiló correctamente. Pasaron **11 grupos de comprobaciones del controlador**, **8 grupos de interacciones** y **5 comprobaciones de regresión de acceso**. Se corrigieron el placeholder superpuesto, la pérdida del primer toque al salir del campo de edad y la conservación del idioma automático. Después se revisaron las cuatro capturas completas; no se construyó una instancia por botón.

| Ventana | Español | English |
| --- | --- | --- |
| Edad/región | [Captura](../../artifacts/visual/WIN-006/age-es-final.png) · [Comparación](../../artifacts/visual/WIN-006/comparison-es.side-by-side.png) | [Capture](../../artifacts/visual/WIN-006/age-en-final.png) · [Comparison](../../artifacts/visual/WIN-006/comparison-en.side-by-side.png) |
| Privacidad | [Captura](../../artifacts/visual/WIN-007/consent-es-final.png) · [Comparación](../../artifacts/visual/WIN-007/comparison-es.side-by-side.png) | [Capture](../../artifacts/visual/WIN-007/consent-en-final.png) · [Comparison](../../artifacts/visual/WIN-007/comparison-en.side-by-side.png) |

[Manifiesto de verificación](../../artifacts/visual/WIN-006/build-verification.json) · [Interacciones y controlador](../../artifacts/visual/WIN-006/age-es-final.smoke.json) · [Regresión de acceso](../../artifacts/visual/WIN-006/welcome-regression.smoke.json).

La composición y los controles siguen las referencias aprobadas. Persisten diferencias en el rasterizado/sombreado de letras y las zonas reconstruidas detrás del texto; la vista previa añade siempre su aviso de datos ficticios. Las métricas completas conservadas son descriptivas y no declaran una coincidencia matemática 1:1. La revisión del usuario y la verificación en dispositivos móviles quedan pendientes.

Se verificó que el APK **0.2.1** anterior conserva su SHA-256. No se generó APK ni binario iOS. El cambio de versión del proyecto a 0.3.0 identifica el reproductor Linux y el nuevo código, no una entrega Android nueva.

## Siguiente ventana

La siguiente propuesta del camino ordinario autenticado es **WIN-009 · Perfil físico privado**, dependiente de WIN-007. **WIN-008 · Tutor** solo corresponde cuando una política regional revisada exige ese proceso; no se inventa un umbral universal. **WIN-004 · Registro por correo** sigue siendo una rama separada para la entrada sin cuenta. Preparar el diseño de WIN-009 no salta estos requisitos en producción ni autoriza recoger datos de salud sin la base aplicable.

La preparación de WIN-009 se limita a propuesta, datos, copia y render retenido para aprobación posterior. No se implementa en esta iteración.
