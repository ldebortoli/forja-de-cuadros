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

## D-007 - Kaggle I2V opcional, privado y reproducible

- Estado: vigente.
- Fecha: 2026-08-20.
- Decisión: integrar Kaggle mediante CLI oficial 2.2.0 y OAuth; crear dataset y kernel privados con identificadores únicos, solicitar T4 y ejecutar LTX-Video 2B 0.9.8 distilled desde un commit fijado. Descargar el MP4, limpiar temporales locales y ofrecer limpieza remota activada por defecto.
- Motivo: aprovechar GPU gratuita compartida sin incrustar credenciales, modelos gigantes ni datos del usuario en el repositorio, y mantener el flujo tradicional enteramente local.

## D-008 - Umbrales de cobertura posteriores a Kaggle

- Estado: vigente; reemplaza D-005.
- Fecha: 2026-08-20.
- Decisión: exigir 78 % de líneas, 48 % de ramas y 72 % de métodos para el núcleo cubierto.
- Motivo: la base verificada después de Kaggle mide 80,61 %, 48,98 % y 73,72 % respectivamente.

## D-009 - Empaquetado Linux y releases diferidos

- Estado: vigente.
- Fecha: 2026-08-20.
- Decisión: no crear todavía ejecutables Linux ni releases descargables; hacerlo cuando el flujo Kaggle tenga una prueba real aprobada.
- Motivo: el usuario pidió probar primero la aplicación actual y posponer la distribución multiplataforma.

## D-010 - OAuth exige confirmar primero el alta de Kaggle

- Estado: vigente.
- Fecha: 2026-08-20.
- Decisión: mostrar dentro del paso 01 una guía numerada para crear la cuenta, confirmar correo y completar la verificación requerida; antes de abrir OAuth, exigir una confirmación explícita y mostrar un aviso si falta.
- Motivo: una conexión técnica no debe comenzar sin que la persona entienda que necesita una cuenta Kaggle creada y habilitada para GPU.

## D-011 - Barras de scroll globales tipo overlay

- Estado: vigente.
- Fecha: 2026-08-20.
- Decisión: reemplazar el template nativo de WPF por un estilo global compacto sin flechas ni canal sólido, con pulgar redondeado verde apagado, mayor presencia en hover y estado de arrastre más oscuro. Aplicarlo a orientación vertical/horizontal y a scrolls internos sin alterar la UI aprobada restante.
- Motivo: las barras nativas claras desentonaban tanto sobre el panel de tinta como sobre las superficies de papel; un overlay sobrio integra ambas zonas sin sumar brillo visual.

## D-012 - Controles de entrada en paleta cacao

- Estado: vigente.
- Fecha: 2026-08-20.
- Decisión: reemplazar los templates claros del sistema por una familia global de superficies cacao mate, bordes marrones y foco verde apagado para TextBox, ComboBox, ComboBoxItem, CheckBox y Slider. Distinguir campos de solo lectura con un marrón más profundo y mantener el popup completo del dropdown dentro de la misma paleta.
- Motivo: los fondos blancos y los controles nativos rompían la continuidad visual de Forja; la nueva familia reduce el brillo sin alterar la identidad aprobada ni sacrificar legibilidad.
