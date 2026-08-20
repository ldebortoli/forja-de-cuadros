# Session handoff

## Objetivo general

Publicar y mantener Forja de Cuadros como herramienta Windows gratuita y abierta, separada del proyecto de juego.

## Tarea actual

Publicación inicial del repositorio público.

## Estado actual

- Rama local: `codex/initial-public-release`.
- Remoto público creado y configurado: `https://github.com/ldebortoli/forja-de-cuadros`.
- .NET 8.0.424 instalado y proyecto migrado.
- Build Release: 0 advertencias, 0 errores.
- Tests: 7/7.
- Cobertura: líneas 76,27 %, ramas 50,57 %, métodos 70,47 %; umbrales 72/48/68.
- Autoprueba FFmpeg: correcta, incluidos 16 cuadros y todos los artefactos.
- Instalador estándar probado en `%LOCALAPPDATA%`; acceso real, iconos, ventana única y cierre normal verificados.
- Captura 1280×680 confirma barra superior y cierre visibles.

## Próximos pasos exactos

1. Ejecutar auditoría de privacidad y secretos sobre archivos versionables.
2. Crear commit inicial con rutas explícitas.
3. Empujar a `main` y verificar CI/seguridad del repositorio público.
4. Marcar esta entrega como DONE y registrar el commit remoto.

## Riesgos

- No publicar `bin/`, `obj/`, `coverage/`, videos, reportes temporales ni datos del usuario.
- La autoprueba FFmpeg de CI es manual para evitar consumo recurrente de cuota.
