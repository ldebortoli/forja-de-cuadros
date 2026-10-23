# TODO

- [P2] Preparar ejecutables Linux y releases descargables. [BLOCKED: el flujo Kaggle ya está probado, pero el usuario lo difirió y todavía no pidió compilar ni publicar esas entregas.]

# IN PROGRESS

# DONE

- [2026-08-25] Agregar `AUTO LOOP`: firmas visuales reducidas, continuidad de velocidad en la costura, mínimo 16 candidatos, selección de 16 fases sin incluir el cierre repetido, confianza alta/media/baja y edición manual intacta. Cinco pruebas nuevas cubren mínimos, ciclo repetido, contenido raster y exclusión del terminal; captura real revisada.
- [2026-08-25] Hacer descartable el splash desde el primer render con `X` visible y `Esc`. Su cierre cancela el temporizador, apaga la app y no abre la principal; prueba instalada confirmó `CANCELLED=1 / MAIN_WINDOW=0` y la captura conserva icono/identidad propios.
- [2026-08-25] Validar e instalar la entrega: build Release 0/0, 19/19 tests, cobertura 86,50 % líneas / 67,70 % ramas / 80,95 % métodos, capturas de splash y mesa, instalaciones Menú Inicio/Codex Apps actualizadas y apertura/cierre real con título propio y código 0.

- [2026-08-22] Agregar en el paso 00 `MOSTRAR CHROMA EN CARPETA`, desactivado hasta que exista el PNG y habilitado al terminar la preparación verde/azul; reutiliza la acción que abre el Explorador con el archivo seleccionado. Validado con build 0/0, 14/14 tests, cobertura 84,84/65,72/78,37, captura compacta e instalación/acceso real con iconos, identidad y cierre normal.

- [2026-08-22] Retirar por completo la integración Kaggle/LTX: borrar ventana, cliente, modelos, tests, captura, documentación y caché local; sustituir el paso 01 por un puente manual a proveedores externos con ruta/copiar/carpeta y Grok Imagine como único aprobado. Documentar candidatos I2V sin agregarlos a la UI antes de la aprobación del usuario. Validado con build 0/0, 14/14 tests, cobertura 84,84/65,72/78,37, autoprueba FFmpeg 5/5, captura compacta, acceso real, iconos, identidad y cierre normal.

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
- [2026-08-22] Corregir el primer workflow Kaggle GPU real: diagnosticar logs UTF-8, descargar modelos secuencialmente CPU/GPU, desactivar Florence/Llama, mantener condicionamiento y VAE en el dispositivo correcto, reescalar en CPU y decodificar 97 cuadros mediante bloques causales solapados. Generación T4 real validada como H.264 512×512, 30 FPS, 97/97 cuadros; temporales y recursos privados eliminados.
- [2026-08-22] Hacer que `VERIFICAR` muestre éxito o fallo persistente y modal, e integrar la cuota oficial `kaggle quota --csv` con porcentaje, horas y reinicio. Lectura real validada; 36/36 tests y cobertura 85,57/51,40/77,35.
- [2026-08-22] Evaluar visualmente el primer MP4 Kaggle real: aunque el pipeline entregó 97 cuadros válidos, LTX 2B deformó identidad, cara, manos y silueta y agregó ruido al chroma. El usuario descartó Kaggle/LTX para las animaciones reales; no consumir más cuota salvo pedido explícito.

- [IN PROGRESS 2026-10-23] R19d: migración compatible .NET10 en copia aislada de Forja mantenida. Baseline .NET8/19tests/86.50%líneas67.70%ramas80.95%métodos pasa umbraleshistóricos78/48/72. Añadir fixturesantesdemigrar ycerrargate100 sin nuevasexclusiones. Rollbackambasinstalaciones/accesos preservado. Snapshotnet5deotrojuegoexcluido.
