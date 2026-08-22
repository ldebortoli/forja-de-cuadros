# Forja de Cuadros: workflow de video a sprites

`Forja de Cuadros` vive en `src/ForjaDeCuadros/`. El instalador crea un acceso directo en el menú Inicio; con `-CodexApps`, usa la carpeta personal `Codex Apps`.

La herramienta separa tres responsabilidades:

- **Preparacion local de imagen:** convierte la transparencia de un PNG en chroma verde o azul sin usar servicios ni creditos.
- **Generacion I2V intercambiable:** produce un MP4 corto desde una imagen aprobada mediante un servicio externo elegido por el usuario. Forja abre solamente proveedores aprobados y no conecta cuentas ni sube archivos.
- **Postprocesado local reproducible:** extrae, selecciona, limpia, registra, audita y exporta los 16 cuadros sin enviar el video a ningun servicio.

Al elegir una imagen, el paso 00 muestra el original y la versión preparada lado a lado, genera chroma verde automáticamente y escribe la ruta resultante en el paso 01; los botones verde/azul regeneran y reemplazan esa misma ruta. Desde allí se puede copiar la ruta, abrir la carpeta o visitar un generador aprobado. El MP4 descargado se carga manualmente en el paso 02. El postprocesado posterior es local.

La lista de proveedores es deliberadamente conservadora: por ahora contiene solamente **Grok Imagine**, aprobado por el usuario. Las alternativas investigadas se agregan únicamente después de una prueba y aprobación explícitas.

## Contrato recomendado para el video fuente

- 2–4 segundos y una sola accion.
- Personaje completo con margen, camara fija y sin zoom.
- Fondo verde o azul uniforme, sin sombra proyectada.
- Misma identidad, ropa y equipo durante todo el clip.
- En loops, pose y velocidad final compatibles con el inicio.

## Salidas

Cada paquete conserva:

- `frames/frame_01.png` a `frame_16.png`.
- Atlas PNG 4x4 u horizontal.
- GIF y `index.html` de revision fija.
- Metadata JSON con hashes, regiones, anchors y auditoria.
- `SpriteFrames.tres` con la ruta `res://` indicada en la UI.
- `LEEME.txt` con resultado de auditoria y ruta esperada por Godot.

El `.tres` no copia automaticamente el atlas dentro del proyecto: primero se revisa el paquete y luego se integra a la ruta declarada. Esto evita reemplazar una animacion aprobada por accidente.

## Ventana y monitores

La ventana usa una barra superior propia con botones persistentes de minimizar, maximizar/restaurar y cerrar. Al iniciarse o pasar a otra pantalla, recalcula el area util de ese monitor, respeta la barra de tareas y reduce el tamano si la resolucion es menor. Los paneles laterales permanecen desplazables y la barra superior no se deshabilita durante un proceso, por lo que cerrar sigue cancelando todo el arbol FFmpeg.
