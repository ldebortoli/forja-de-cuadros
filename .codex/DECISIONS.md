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

## D-013 - No esperar CI remota por defecto

- Estado: vigente.
- Fecha: 2026-08-20.
- Decisión: después de validar localmente y hacer push, no ejecutar `watch`, no sondear ni esperar GitHub Actions salvo que el usuario lo pida explícitamente en la solicitud actual.
- Motivo: esperar una ejecución remota consume tiempo y tokens sin aportar valor al flujo habitual; el push puede quedar con CI pendiente.

## D-014 - Imagen transparente como inicio local del workflow

- Estado: vigente.
- Fecha: 2026-08-21.
- Decisión: iniciar la UI en `00 GENERAR IMAGEN`, aplanar localmente PNG/formatos WIC transparentes sobre chroma verde o azul y entregar el PNG opaco resultante preseleccionado a `01 CONVERTIR A VIDEO` con Kaggle; `02 VIDEO` recibe el MP4. Unificar a 40 px la altura mínima de inputs y filas de acción, y usar tres celdas de ventana idénticas de 46 × 40 px en principal y Kaggle.
- Motivo: convertir transparencia en un fondo uniforme no necesita IA ni créditos, evita halos en bordes semitransparentes y vuelve explícito el recorrido imagen → video → cuadros. Las métricas fijas eliminan las disparidades visuales señaladas por el usuario.

## D-015 - Publicación directa a main sin PR

- Estado: vigente.
- Fecha: 2026-08-21.
- Decisión: para Forja, integrar y empujar los cambios directamente a `main`; no crear pull requests ni ramas de entrega salvo pedido explícito del usuario.
- Motivo: el usuario prefiere que las entregas terminadas queden disponibles inmediatamente en la rama pública principal y pidió expresamente no usar PRs.

## D-016 - Handoff visible y automático de archivos

- Estado: vigente.
- Fecha: 2026-08-21.
- Decisión: al elegir una imagen en 00, generar chroma verde inmediatamente y rellenar con su ruta el campo de entrada visible de 01; regenerar y reemplazarla si se elige chroma azul. Mantener el MP4 devuelto por Kaggle como carga automática del campo de video de 02.
- Motivo: cada paso que produce un archivo debe completar el consumidor siguiente sin obligar a buscar de nuevo el mismo archivo ni ocultar qué ruta se está usando.

## D-017 - Corte alfa en dos pasadas con previsualización

- Estado: vigente.
- Fecha: 2026-08-21.
- Decisión: añadir al paso 03 un corte alfa activado por defecto en 10 %, un suavizado independiente en 4 % y una previsualización sobre damero. Aplicar la máscara después del chroma antes de medir límites y repetirla después del escalado/remuestreo.
- Motivo: los píxeles de alfa muy bajo forman halos y además falsean el encuadre; la segunda pasada limpia transparencias débiles reintroducidas por interpolación sin obligar a destruir detalle con un umbral duro.

## D-018 - Identidad Kaggle automática y dataset transitorio compatible

- Estado: vigente; reemplaza la versión de CLI y la entrada manual de usuario de D-007.
- Fecha: 2026-08-22.
- Decisión: fijar Kaggle CLI 2.2.2 con actualización automática, detectar el usuario OAuth mediante `kaggle config view`, sobrescribir el slug manual con la identidad autenticada, declarar el dataset privado temporal con licencia `other` y tratar mensajes `Dataset creation error`, `Kernel push error` o HTTP 403 como fallos aunque la CLI devuelva código 0.
- Motivo: la primera prueba real confirmó que la identidad ingresada coincidía con OAuth, pero el endpoint rechazó `copyright-authors`; la CLI continuó y produjo un 403 secundario. Una prueba privada mínima con `other` terminó en estado `ready` y se eliminó correctamente sin usar GPU.

## D-019 - Checkmark estable y doble visor en el paso 00

- Estado: vigente.
- Fecha: 2026-08-22.
- Decisión: renderizar el checkmark como un path no estirado, centrado dentro de una caja redondeada de 18 px, y mostrar en el paso 00 dos visores gemelos sobre damero para la imagen original y el chroma que recibirá Kaggle.
- Motivo: el escalado automático deformaba el tilde y no existía confirmación visual de la imagen cargada ni del archivo preparado que se entregaba al paso siguiente.
