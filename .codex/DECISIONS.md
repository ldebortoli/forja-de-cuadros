# Decisiones tecnicas

No borrar decisiones anteriores. Si una decision cambia, agregar una nueva entrada que indique cual reemplaza.

## D-001 - Memoria persistente del proyecto

- Estado: vigente.
- Fecha: 2026-08-20.
- Decision: usar `.codex/` como fuente de verdad entre sesiones, modelos y agentes.
- Motivo: continuidad independiente del historial del chat.

## D-002 - Repositorio público independiente y licencia MIT

- Estado: vigente.
- Fecha: 2026-08-20.
- Decisión: mantener Forja de Cuadros en `ldebortoli/forja-de-cuadros`, separado del juego, con licencia MIT.
- Motivo: permitir compartir y mantener la herramienta sin publicar assets ni historia privada del juego.

## D-003 - Base soportada en .NET 8

- Estado: vigente.
- Fecha: 2026-08-20.
- Decisión: apuntar a `net8.0-windows` y usar el AppUserModelID `io.github.ldebortoli.ForjaDeCuadros`.
- Motivo: .NET 5 está fuera de soporte; el identificador público debe ser estable y propio del repositorio.

## D-004 - CI rápida y autoprueba costosa manual

- Estado: vigente.
- Fecha: 2026-08-20.
- Decisión: ejecutar build, xUnit y cobertura en push/PR; reservar la pipeline completa con FFmpeg para `workflow_dispatch`.
- Motivo: proteger minutos gratuitos sin perder una verificación integral disponible bajo demanda.

## D-005 - Umbrales de cobertura basados en la medición inicial

- Estado: vigente.
- Fecha: 2026-08-20.
- Decisión: exigir 72 % de líneas, 48 % de ramas y 68 % de métodos para el núcleo cubierto.
- Motivo: la base verificada mide 76,27 %, 50,57 % y 70,47 % respectivamente; los umbrales previenen regresiones con margen pequeño.

## D-006 - Acciones oficiales compatibles con Node 24

- Estado: vigente.
- Fecha: 2026-08-20.
- Decisión: usar `checkout@v7`, `setup-dotnet@v6`, `cache@v6` y `upload-artifact@v7`.
- Motivo: son las versiones oficiales actuales y eliminan la advertencia de deprecación de Node 20 emitida por los runners de GitHub.
