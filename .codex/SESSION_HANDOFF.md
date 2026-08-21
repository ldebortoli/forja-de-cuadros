# Session handoff

## Objetivo general

Mantener Forja de Cuadros como herramienta Windows gratuita y abierta, separada del proyecto de juego.

## Tarea actual

Esperar a que el usuario siga la guía integrada, cree/verifique su cuenta y ejecute el primer trabajo Kaggle GPU real. Linux y releases ejecutables continúan diferidos.

## Estado actual

- Repositorio público: `https://github.com/ldebortoli/forja-de-cuadros`.
- Rama primaria: `main`.
- Entrega inicial publicada con licencia MIT y documentación bilingüe breve.
- .NET 8.0.424; build Release con 0 advertencias y 0 errores.
- Asistente Kaggle I2V integrado: alta/verificación, OAuth, CLI 2.2.0 aislada, job privado T4, espera, descarga y limpieza.
- El paso 01 ahora contiene una guía numerada y confirmación explícita; un intento de conectar antes de completarla muestra un aviso y no abre OAuth.
- Las barras de scroll verticales, horizontales e internas comparten un template overlay sin canal sólido, con pulgar redondeado verde apagado y hover/drag discretos; el resto del diseño no cambió.
- TextBox, ComboBox y su popup, ComboBoxItem, CheckBox y Slider comparten ahora superficies cacao mate, borde marrón y foco verde apagado; los campos de solo lectura usan un marrón algo más profundo y ya no aparecen superficies blancas del sistema.
- CLI 2.2.0 instalada y su sintaxis actual contrastada localmente; falta únicamente el job remoto por no existir todavía la cuenta del usuario.
- Tests locales y CI pública: 17/17; el estilo global de scroll pasó el run `32430904319`.
- Cobertura: líneas 80,61 %, ramas 48,76 %, métodos 73,72 %; umbrales 78/48/72.
- Autoprueba FFmpeg: correcta, incluidos 16 cuadros y todos los artefactos.
- Instalador estándar probado en `%LOCALAPPDATA%`; acceso real, iconos, AppUserModelID, ventana única y cierre normal verificados.
- Capturas principal 1060×680/1280×900 y Kaggle 900×640 confirman barra superior, cierre, scrollbars y controles de entrada coherentes sobre fondos oscuros y claros.
- Gitleaks: cero hallazgos en archivos e historial.
- Secret Scanning, Push Protection, alertas y actualizaciones automáticas de seguridad activos.
- CI usa versiones oficiales de GitHub Actions compatibles con Node 24.

## Próximos pasos

1. En Forja: `KAGGLE I2V` y seguir la guía visible del paso 01 para crear/verificar la cuenta.
2. Marcar la confirmación → `CONECTAR CUENTA` → `VERIFICAR` → generar con una imagen de prueba.
3. Si el MP4 se aprueba, preparar después Linux y releases cuando el usuario lo pida.

## Riesgos

- No publicar `bin/`, `obj/`, `coverage/`, videos, reportes temporales ni datos del usuario.
- La autoprueba FFmpeg de CI es manual para evitar consumo recurrente de cuota.
- El primer job Kaggle puede esperar cola y descargar dependencias/pesos; no afirmar validación GPU real hasta que se ejecute con la cuenta del usuario.
- Computer Use contra Explorer falló en agosto de 2026 con `Interfaz no compatible (0x80004002)` y geometría no disponible. Fallback validado: captura interna de la app, AppUserModelID y propiedades del acceso. No reintentar antes de septiembre salvo cambio de versión/configuración.
