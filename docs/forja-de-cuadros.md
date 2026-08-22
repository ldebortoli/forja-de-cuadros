# Forja de Cuadros: workflow de video a sprites

`Forja de Cuadros` vive en `src/ForjaDeCuadros/`. El instalador crea un acceso directo en el menú Inicio; con `-CodexApps`, usa la carpeta personal `Codex Apps`.

La herramienta separa tres responsabilidades:

- **Preparacion local de imagen:** convierte la transparencia de un PNG en chroma verde o azul sin usar servicios ni creditos.
- **Generacion I2V intercambiable:** produce un MP4 corto desde una imagen aprobada. El asistente integrado usa Kaggle/LTX; también se puede importar manualmente un clip de Grok u otra fuente.
- **Postprocesado local reproducible:** extrae, selecciona, limpia, registra, audita y exporta los 16 cuadros sin enviar el video a ningun servicio.

Al elegir una imagen, el paso 00 muestra el original y la versión preparada lado a lado, genera chroma verde automáticamente y escribe la ruta resultante en el paso 01; los botones verde/azul regeneran y reemplazan esa misma ruta. `ABRIR KAGGLE I2V` la entrega preseleccionada al asistente y, cuando vuelve el MP4 aprobado, el paso 02 carga su ruta automáticamente. Kaggle automatiza OAuth oficial, detección del usuario, dataset/kernel privados, GPU T4, espera y descarga del MP4. El postprocesado posterior no usa la nube. Ver `docs/kaggle.md`.

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
