# Session handoff

## Objetivo general

Mantener Forja de Cuadros como herramienta Windows gratuita y abierta, separada del proyecto de juego.

## Tarea actual

Esperar la primera prueba real del usuario con una animación generada. La prueba GPU Kaggle y Linux/releases continúan diferidos.

## Estado actual

- Repositorio público: `https://github.com/ldebortoli/forja-de-cuadros`.
- Rama primaria: `main`.
- El recorrido imagen → video quedó fusionado directamente en `main` hasta `c04f2d0`; GitHub marcó el PR borrador preexistente como fusionado y la rama auxiliar fue eliminada local y remotamente. No se esperó ni monitoreó la CI.
- Entrega inicial publicada con licencia MIT y documentación bilingüe breve.
- .NET 8.0.424; build Release con 0 advertencias y 0 errores.
- Asistente Kaggle I2V integrado: alta/verificación, OAuth, CLI 2.2.0 aislada, job privado T4, espera, descarga y limpieza.
- El paso 01 ahora contiene una guía numerada y confirmación explícita; un intento de conectar antes de completarla muestra un aviso y no abre OAuth.
- Las barras de scroll verticales, horizontales e internas comparten un template overlay sin canal sólido, con pulgar redondeado verde apagado y hover/drag discretos; el resto del diseño no cambió.
- TextBox, ComboBox y su popup, ComboBoxItem, CheckBox y Slider comparten ahora superficies cacao mate, borde marrón y foco verde apagado; los campos de solo lectura usan un marrón algo más profundo y ya no aparecen superficies blancas del sistema.
- La navegación principal ahora es `00 GENERAR IMAGEN` → `01 CONVERTIR A VIDEO` → `02 VIDEO` → `03 FONDO / CHROMA` → `04 REGISTRO` → `05 EXPORTACIÓN`.
- El paso 00 elige PNG/JPG/WebP/WIC y convierte localmente la transparencia a chroma verde o azul, incluyendo composición correcta de bordes alfa; Kaggle se abre con ese PNG ya preseleccionado.
- Elegir la imagen dispara chroma verde sin un segundo clic y escribe su ruta en un campo visible del paso 01; pulsar verde o azul regenera el PNG y reemplaza ese campo. Si la preparación faltara, abrir Kaggle la reintenta antes de avanzar.
- El handoff 01 → 02 ya existente se conserva: `USAR ESTE VIDEO` cierra Kaggle, verifica el MP4 y carga su ruta en el campo de video principal.
- El paso 03 suma `Limpiar halo con corte alfa`, activo en 10 %, y `Suavizado del corte`, activo en 4 %. Ambos sliders muestran su valor y actualizan una previsualización sobre damero del cuadro elegido.
- El corte alfa se aplica después del chroma antes de medir límites/alineación y se repite después del escalado para eliminar transparencia débil reintroducida por interpolación. Los originales no se modifican y la exportación registra ambos parámetros.
- Inputs y dropdowns tienen una altura mínima común de 40 px; los botones de filas emparejadas también usan 40 px. Principal y Kaggle tienen minimizar, maximizar/restaurar y cerrar en celdas idénticas de 46 × 40 px.
- CLI 2.2.0 instalada y su sintaxis actual contrastada localmente; falta únicamente el job remoto por no existir todavía la cuenta del usuario.
- Tests locales: 24/24. La CI remota posterior al push no se espera ni monitorea por política.
- Cobertura: líneas 81,14 %, ramas 50,40 %, métodos 76,00 %; umbrales 78/48/72.
- Autoprueba FFmpeg: correcta, incluidos 16 cuadros y todos los artefactos.
- Instalador estándar probado en `%LOCALAPPDATA%`; acceso real, iconos, AppUserModelID, ventana única y cierre normal verificados.
- La captura interna `--capture-alpha` 1440×960 enfoca el panel completo: controles cacao/turquesa, valores 10/4, ayuda, damero y barra superior fija sin romper el layout. Las capturas anteriores principal/Kaggle siguen vigentes.
- Instalación personal actualizada desde el repositorio; acceso real en `Codex Apps`, destino/icono, ventana única, cierre normal y AppUserModelID de proceso `io.github.ldebortoli.ForjaDeCuadros` verificados.
- Gitleaks: cero hallazgos en archivos e historial.
- Secret Scanning, Push Protection, alertas y actualizaciones automáticas de seguridad activos.
- CI usa versiones oficiales de GitHub Actions compatibles con Node 24.
- Por regla global y del proyecto, después de cada push no se espera ni monitorea GitHub Actions salvo pedido explícito; se entrega con validación local y la CI puede quedar pendiente.
- Por instrucción del usuario, las próximas entregas de Forja se publican directamente en `main`, sin PR ni rama auxiliar salvo pedido explícito.

## Próximos pasos

1. En Forja: elegir un PNG transparente en el paso 00, preparar chroma verde/azul y abrir Kaggle en el paso 01.
2. Seguir la guía visible para crear/verificar la cuenta; marcar la confirmación → `CONECTAR CUENTA` → `VERIFICAR` → generar.
3. Confirmar que el MP4 vuelve al paso 02 y sirve para extraer candidatos; elegir uno y ajustar el corte alfa hasta perder el halo sin cortar detalles, compensando serrucho con suavizado.
4. Preparar Linux/releases sólo cuando el usuario lo pida después de esa prueba real.

## Riesgos

- No publicar `bin/`, `obj/`, `coverage/`, videos, reportes temporales ni datos del usuario.
- La autoprueba FFmpeg de CI es manual para evitar consumo recurrente de cuota.
- El primer job Kaggle puede esperar cola y descargar dependencias/pesos; no afirmar validación GPU real hasta que se ejecute con la cuenta del usuario.
- Computer Use contra Explorer y Forja WPF falló en agosto de 2026 con `Interfaz no compatible (0x80004002)` y geometría no disponible. Fallback validado: captura interna de la app, AppUserModelID y propiedades del acceso. No reintentar antes de septiembre salvo cambio de versión/configuración.
