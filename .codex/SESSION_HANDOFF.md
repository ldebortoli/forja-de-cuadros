# Session handoff

## Objetivo general

Mantener Forja de Cuadros como herramienta Windows gratuita y abierta, separada del proyecto de juego.

## Tarea actual

`AUTO LOOP` y el cierre seguro del splash quedaron implementados, validados e instalados. El siguiente paso útil es que el usuario pruebe la detección con su primer MP4 real y ajuste la sensibilidad sólo si un caso concreto produce una costura incorrecta.

La integracion Kaggle/LTX fue retirada por completo por instruccion del usuario. El paso 01 ahora es un puente manual a proveedores I2V externos: conserva el PNG croma, permite copiar la ruta o abrir su carpeta y abre solamente proveedores aprobados. Grok Imagine es el unico aprobado actualmente. El MP4 se descarga fuera de Forja y se carga en `02 VIDEO`.

## Estado actual

- Repositorio publico: `https://github.com/ldebortoli/forja-de-cuadros`.
- Rama primaria y de entrega: `main`; no usar PR salvo pedido explicito.
- Entrega actual publicada directamente en `main` mediante `f136f3c`; no se creó PR y no se esperó ni monitoreó la CI remota.
- .NET 8, C#, WPF/WinForms interop, Windows x64.
- Flujo visible: `00 GENERAR IMAGEN` → `01 CONVERTIR A VIDEO` → `02 VIDEO` → `03 FONDO / CHROMA` → `04 REGISTRO` → `05 EXPORTACION`.
- Paso 00: dos visores `ORIGINAL`/`CHROMA PARA VIDEO`; seleccionar imagen genera verde `#00FF00` automaticamente y verde/azul reemplazan la ruta de 01. Debajo del resultado, `MOSTRAR CHROMA EN CARPETA` se habilita solamente cuando el PNG preparado existe y lo selecciona en el Explorador.
- Paso 01: no conecta cuentas, no ejecuta modelos y no sube archivos. Incluye `COPIAR RUTA`, `ABRIR CARPETA` y `ABRIR GROK`; la tarjeta indica Grok Imagine como aprobado.
- Paso 02: el MP4/MOV/WebM/GIF se elige manualmente. Extraccion, chroma, corte alfa, registro, auditoria y exportacion siguen locales.
- La mesa ofrece `AUTO LOOP`: requiere 16 candidatos, busca inicio/cierre mediante firma visual y continuidad, excluye el cierre repetido, selecciona 16 fases y muestra confianza; `AUTO 16` uniforme y la edición manual permanecen.
- El splash tiene `X` y `Esc`; la prueba de descarte confirmó que no crea la ventana principal.
- `docs/proveedores-i2v.md` compara Grok, Google Flow, Firefly, Pika, PixVerse, Runway y Luma. Los candidatos no aparecen en la UI antes de la aprobacion del usuario.
- La barra superior propia conserva tres celdas de 46 x 40 px y queda accesible en monitores compactos. Inputs y botones de fila usan altura minima 40 px.
- Tema, scrollbars overlay, checks centrados y controles cacao permanecen sin cambios.
- Cerrar la ventana cancela el arbol FFmpeg activo.
- Validacion local: build Release con 0 advertencias/0 errores; 19/19 tests; cobertura 86,50 % lineas, 67,70 % ramas y 80,95 % metodos frente a umbrales 78/48/72; descarte de splash `CANCELLED=1 / MAIN_WINDOW=0`; la autoprueba FFmpeg previa permanece en 5/5.
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

## R19d preparación completada sobre.NET8 - 2026-10-23
Worktree audit/r19d-net10-20261023 desde9368700, appinterna1.0.1 aúnNOadoptada. Baselineoriginal19tests/86.50,67.70,80.95; tres testsrojos demostraronadiciónRGBenchroma yfixmínimo pasa.68tests/100%líneasramasmétodosnúcleoinstrumentado; exclusionesoriginalesintactas. Cuatro checksinalcanzablesretiradosconprecondicionesdocumentadas. AutopruebaFFmpeg5/5 con mediossintéticos,16cuadros/hashesguardados; capturadeUI8exit0sóloenFORJA_DATA_DIRECTORYtemporal. Rollbackambasinstalaciones/accesosconservadoenTemp/codex-r19-net10-20261023/rollback/forja. Falta migrarSDK10.0.402/runtime10.0.13, compararpruebas/pipeline/UI, publicar/adoptar/subirnormal. Snapshotnet5juegoexcluido. No aprobaciónpendiente,nidenegación.

## R19d.NET10 validaciónunitaria terminada - 2026-10-23
Candidato1.0.2/SDK10.0.402/Runtime10.0.13/C#12: restore/build0warnings y68tests100%561líneas/372ramas/métodos. NuGet sin hallazgos, paquetes existentes compatibles. Etapa previaf120d95 mantienearregloRGB/fixtures/gate100 ydatosQA. Falta publicarcommitfinal, correr autoprueba/capturas/splash/identidad/cierre delpaquete autocontenido y comparar16PNGconbaselinecorregido.NET8. Sólo si pasa adoptarambasinstalacionesversionadasyaccesos, commitmemoriaDONE/merge/pushnormal. Sinuso deproveedores/cuentas/mediosreales. Rollbackpreservado.
