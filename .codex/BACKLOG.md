# TODO

- [P1] Probar una generación Kaggle GPU de punta a punta. [BLOCKED: la cuenta, OAuth, CLI y dataset privado ya están validados; falta que el usuario pulse SINCRONIZAR Y GENERAR o autorice expresamente consumir cuota GPU.]
- [P2] Preparar ejecutables Linux y releases descargables. [BLOCKED: el usuario lo difirió hasta que el flujo Kaggle esté probado.]

# IN PROGRESS

- Sin tareas activas.

# DONE

- [2026-08-20] Inicializar memoria persistente y estructura Git independiente.
- [2026-08-20] Migrar la aplicación de .NET 5 a .NET 8 y asignar identidad pública estable.
- [2026-08-20] Generalizar el instalador para menú Inicio y conservar `-CodexApps` como opción.
- [2026-08-20] Agregar tests xUnit, cobertura con umbrales y workflows de CI eficientes.
- [2026-08-20] Corregir la deriva de suelo causada por el remuestreo bilineal.
- [2026-08-20] Validar build limpio, 7/7 tests, cobertura, autoprueba FFmpeg e instalación/identidad/cierre desde el acceso real.
- [2026-08-20] Publicar la entrega inicial en `https://github.com/ldebortoli/forja-de-cuadros` con rama primaria `main`, CI verde y controles de seguridad activos.
- [2026-08-20] Actualizar GitHub Actions a las versiones oficiales compatibles con Node 24.
- [2026-08-20] Integrar Kaggle I2V: onboarding y OAuth, CLI oficial aislada, dataset/kernel privados T4 con LTX-Video fijado, espera, descarga, limpieza, UI compacta, 17 tests y documentación pública.
- [2026-08-20] Reforzar el alta Kaggle dentro del asistente con guía numerada, enlaces de cuenta/verificación, confirmación explícita y bloqueo con aviso antes de OAuth; captura instalada 900×640, identidad y 17 tests correctos.
- [2026-08-20] Reemplazar globalmente los scrollbars WPF por un diseño overlay compacto, redondeado y verde apagado, sin canal claro ni flechas; capturas principal/Kaggle, instalación, identidad y 17 tests correctos.
- [2026-08-20] Reemplazar campos, dropdowns y su popup, checks y sliders por una familia cacao mate con foco verde; capturas compacta/amplia, instalación, identidad, cierre normal y 17 tests correctos.
- [2026-08-20] Registrar globalmente y en Forja que la CI remota no se espera ni monitorea después del push salvo pedido explícito.
- [2026-08-21] Reordenar el flujo visual a 00 imagen, 01 conversión Kaggle y 02 video; preparar transparencia sobre chroma local con entrega automática a Kaggle, unificar alturas de controles y uniformar los botones de ventana principal/Kaggle. Validado con 21 tests, cobertura 81,33/50,31/75,34, capturas e instalación real.
- [2026-08-21] Completar el handoff de archivos: elegir imagen en 00 genera chroma verde y rellena la ruta visible de 01; verde/azul la reemplazan y el MP4 de Kaggle continúa cargándose en 02. Validado con 21 tests, cobertura 81,33/50,21/75,34, captura e instalación real.
- [2026-08-21] Integrar corte alfa en dos pasadas y suavizado posterior al chroma en el paso 03, con sliders horizontales, valores visibles, ayuda y previsualización sobre damero. Validado con captura enfocada, 24/24 tests, cobertura 81,14/50,40/76,00, autoprueba FFmpeg, instalación real, identidad propia y cierre normal.
- [2026-08-22] Corregir el checkmark, agregar visores `ORIGINAL`/`CHROMA PARA KAGGLE`, actualizar Kaggle CLI a 2.2.2, detectar automáticamente la identidad OAuth y reparar la creación del dataset privado con licencia `other` y parada temprana ante errores reportados con exit code 0. Validado con 28/28 tests, cobertura 81,14/49,90/76,00, autoprueba FFmpeg, capturas, prueba Kaggle privada creada/lista/eliminada sin GPU e instalación real.
