# Session handoff

## Objetivo general

Mantener Forja de Cuadros como herramienta Windows gratuita y abierta, separada del proyecto de juego.

## Tarea actual

Esperar a que el usuario pruebe el nuevo recorrido con una imagen real y complete, cuando quiera, el primer trabajo Kaggle GPU. Linux y releases ejecutables continúan diferidos.

## Estado actual

- Repositorio público: `https://github.com/ldebortoli/forja-de-cuadros`.
- Rama primaria: `main`.
- Cambios de este recorrido publicados en `codex/image-video-workflow`, commit `49a4954`, con PR borrador `https://github.com/ldebortoli/forja-de-cuadros/pull/1`; no se esperó ni monitoreó la CI.
- Entrega inicial publicada con licencia MIT y documentación bilingüe breve.
- .NET 8.0.424; build Release con 0 advertencias y 0 errores.
- Asistente Kaggle I2V integrado: alta/verificación, OAuth, CLI 2.2.0 aislada, job privado T4, espera, descarga y limpieza.
- El paso 01 ahora contiene una guía numerada y confirmación explícita; un intento de conectar antes de completarla muestra un aviso y no abre OAuth.
- Las barras de scroll verticales, horizontales e internas comparten un template overlay sin canal sólido, con pulgar redondeado verde apagado y hover/drag discretos; el resto del diseño no cambió.
- TextBox, ComboBox y su popup, ComboBoxItem, CheckBox y Slider comparten ahora superficies cacao mate, borde marrón y foco verde apagado; los campos de solo lectura usan un marrón algo más profundo y ya no aparecen superficies blancas del sistema.
- La navegación principal ahora es `00 GENERAR IMAGEN` → `01 CONVERTIR A VIDEO` → `02 VIDEO` → `03 FONDO / CHROMA` → `04 REGISTRO` → `05 EXPORTACIÓN`.
- El paso 00 elige PNG/JPG/WebP/WIC y convierte localmente la transparencia a chroma verde o azul, incluyendo composición correcta de bordes alfa; Kaggle se abre con ese PNG ya preseleccionado.
- Inputs y dropdowns tienen una altura mínima común de 40 px; los botones de filas emparejadas también usan 40 px. Principal y Kaggle tienen minimizar, maximizar/restaurar y cerrar en celdas idénticas de 46 × 40 px.
- CLI 2.2.0 instalada y su sintaxis actual contrastada localmente; falta únicamente el job remoto por no existir todavía la cuenta del usuario.
- Tests locales: 21/21. La CI remota del último push no se espera ni monitorea por política.
- Cobertura: líneas 81,33 %, ramas 50,31 %, métodos 75,34 %; umbrales 78/48/72.
- Autoprueba FFmpeg: correcta, incluidos 16 cuadros y todos los artefactos.
- Instalador estándar probado en `%LOCALAPPDATA%`; acceso real, iconos, AppUserModelID, ventana única y cierre normal verificados.
- Capturas principal 1280×900 y Kaggle 900×640 confirman el nuevo flujo, la barra superior de tres botones iguales, el cierre, scrollbars y controles de entrada coherentes sobre fondos oscuros y claros.
- Instalación personal actualizada desde el repositorio; acceso real, destino/icono, ventana única, cierre normal y AppUserModelID `io.github.ldebortoli.ForjaDeCuadros` verificados.
- Gitleaks: cero hallazgos en archivos e historial.
- Secret Scanning, Push Protection, alertas y actualizaciones automáticas de seguridad activos.
- CI usa versiones oficiales de GitHub Actions compatibles con Node 24.
- Por regla global y del proyecto, después de cada push no se espera ni monitorea GitHub Actions salvo pedido explícito; se entrega con validación local y la CI puede quedar pendiente.

## Próximos pasos

1. En Forja: elegir un PNG transparente en el paso 00, preparar chroma verde/azul y abrir Kaggle en el paso 01.
2. Seguir la guía visible para crear/verificar la cuenta; marcar la confirmación → `CONECTAR CUENTA` → `VERIFICAR` → generar.
3. Confirmar que el MP4 vuelve al paso 02 y sirve para extraer candidatos; después preparar Linux/releases sólo cuando el usuario lo pida.

## Riesgos

- No publicar `bin/`, `obj/`, `coverage/`, videos, reportes temporales ni datos del usuario.
- La autoprueba FFmpeg de CI es manual para evitar consumo recurrente de cuota.
- El primer job Kaggle puede esperar cola y descargar dependencias/pesos; no afirmar validación GPU real hasta que se ejecute con la cuenta del usuario.
- Computer Use contra Explorer falló en agosto de 2026 con `Interfaz no compatible (0x80004002)` y geometría no disponible. Fallback validado: captura interna de la app, AppUserModelID y propiedades del acceso. No reintentar antes de septiembre salvo cambio de versión/configuración.
