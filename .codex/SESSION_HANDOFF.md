# Session handoff

## Objetivo general

Mantener Forja de Cuadros como herramienta Windows gratuita y abierta, separada del proyecto de juego.

## Tarea actual

No hay una tarea activa registrada.

## Estado actual

- Repositorio público: `https://github.com/ldebortoli/forja-de-cuadros`.
- Rama primaria: `main`.
- Entrega inicial publicada con licencia MIT y documentación bilingüe breve.
- .NET 8.0.424; build Release con 0 advertencias y 0 errores.
- Tests locales y remotos: 7/7.
- Cobertura: líneas 76,27 %, ramas 50,57 %, métodos 70,47 %; umbrales 72/48/68.
- Autoprueba FFmpeg: correcta, incluidos 16 cuadros y todos los artefactos.
- Instalador estándar probado en `%LOCALAPPDATA%`; acceso real, iconos, AppUserModelID, ventana única y cierre normal verificados.
- Captura 1280×680 confirma barra superior y cierre visibles.
- Gitleaks: cero hallazgos en archivos e historial.
- Secret Scanning, Push Protection, alertas y actualizaciones automáticas de seguridad activos.
- CI usa versiones oficiales de GitHub Actions compatibles con Node 24.

## Próximos pasos

No hay pasos obligatorios. La autoprueba FFmpeg puede ejecutarse manualmente desde Actions cuando cambie la pipeline multimedia.

## Riesgos

- No publicar `bin/`, `obj/`, `coverage/`, videos, reportes temporales ni datos del usuario.
- La autoprueba FFmpeg de CI es manual para evitar consumo recurrente de cuota.
