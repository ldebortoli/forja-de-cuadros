# Session handoff

## Objetivo general

Mantener Forja de Cuadros como herramienta Windows gratuita y abierta, separada del proyecto de juego.

## Tarea actual

La integracion Kaggle/LTX fue retirada por completo por instruccion del usuario. El paso 01 ahora es un puente manual a proveedores I2V externos: conserva el PNG croma, permite copiar la ruta o abrir su carpeta y abre solamente proveedores aprobados. Grok Imagine es el unico aprobado actualmente. El MP4 se descarga fuera de Forja y se carga en `02 VIDEO`.

## Estado actual

- Repositorio publico: `https://github.com/ldebortoli/forja-de-cuadros`.
- Rama primaria y de entrega: `main`; no usar PR salvo pedido explicito.
- Entrega funcional publicada directamente en `main` mediante `8d0cf50`; no se creo PR y no se espero ni monitoreo CI remota.
- .NET 8, C#, WPF/WinForms interop, Windows x64.
- Flujo visible: `00 GENERAR IMAGEN` → `01 CONVERTIR A VIDEO` → `02 VIDEO` → `03 FONDO / CHROMA` → `04 REGISTRO` → `05 EXPORTACION`.
- Paso 00: dos visores `ORIGINAL`/`CHROMA PARA VIDEO`; seleccionar imagen genera verde `#00FF00` automaticamente y verde/azul reemplazan la ruta de 01. Debajo del resultado, `MOSTRAR CHROMA EN CARPETA` se habilita solamente cuando el PNG preparado existe y lo selecciona en el Explorador.
- Paso 01: no conecta cuentas, no ejecuta modelos y no sube archivos. Incluye `COPIAR RUTA`, `ABRIR CARPETA` y `ABRIR GROK`; la tarjeta indica Grok Imagine como aprobado.
- Paso 02: el MP4/MOV/WebM/GIF se elige manualmente. Extraccion, chroma, corte alfa, registro, auditoria y exportacion siguen locales.
- `docs/proveedores-i2v.md` compara Grok, Google Flow, Firefly, Pika, PixVerse, Runway y Luma. Los candidatos no aparecen en la UI antes de la aprobacion del usuario.
- La barra superior propia conserva tres celdas de 46 x 40 px y queda accesible en monitores compactos. Inputs y botones de fila usan altura minima 40 px.
- Tema, scrollbars overlay, checks centrados y controles cacao permanecen sin cambios.
- Cerrar la ventana cancela el arbol FFmpeg activo.
- Validacion local: build Release con 0 advertencias/0 errores; 14/14 tests; cobertura 84,84 % lineas, 65,72 % ramas y 78,37 % metodos frente a umbrales 78/48/72; la autoprueba FFmpeg previa permanece en 5/5.
- Captura interna compacta revisada con el nuevo boton. Las instalaciones de menu Inicio y `Codex Apps` estan actualizadas; el acceso real abre una sola ventana, usa iconos propios, declara `io.github.ldebortoli.ForjaDeCuadros` y cierra normalmente.
- El entorno/caché heredado bajo `%LOCALAPPDATA%\ForjaDeCuadros` fue retirado; no se borraron videos ni contenido del usuario.
- El instalador estandar usa `%LOCALAPPDATA%\Programs\Forja de Cuadros`; `-CodexApps` actualiza el acceso personal sin consola y con icono/AppUserModelID propios.
- Secret Scanning, Push Protection, alertas y actualizaciones automaticas de seguridad permanecen activos en GitHub.
- Despues del push no se espera ni monitorea CI remota salvo pedido explicito.

## Proximos pasos

1. Recibir la aprobacion o rechazo del usuario para Google Flow, Adobe Firefly, Pika o PixVerse despues de probar la misma placa croma.
2. Agregar a la tarjeta/lista de 01 solamente los proveedores aprobados expresamente.
3. Preparar Linux/releases solo cuando el usuario lo pida.

## Riesgos

- Las cuotas, marcas de agua y licencias de los proveedores cambian; revisar fuentes oficiales antes de actualizar la lista publica.
- No publicar `bin/`, `obj/`, `coverage/`, videos, reportes temporales, contenido del usuario ni rutas personales.
- Computer Use contra Explorer y Forja WPF fallo en agosto de 2026 con `Interfaz no compatible (0x80004002)`. Usar captura interna, identidad de proceso y propiedades del acceso; no reintentar antes de septiembre salvo cambio de version/configuracion.
