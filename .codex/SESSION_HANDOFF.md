# Session handoff

## Objetivo general

Mantener Forja de Cuadros como herramienta Windows gratuita y abierta, separada del proyecto de juego.

## Tarea actual

Esperar que el usuario pulse `SINCRONIZAR Y GENERAR` para la primera prueba GPU real. El alta, OAuth, dataset privado y UI ya están corregidos; Linux/releases continúa diferido.

## Estado actual

- Repositorio público: `https://github.com/ldebortoli/forja-de-cuadros`.
- Rama primaria: `main`.
- `main` quedó publicado hasta `6aca85e`, que corrige la primera prueba Kaggle y agrega los visores del paso 00 sobre el recorrido imagen → video. No se creó PR ni se esperó o monitoreó la CI.
- Entrega inicial publicada con licencia MIT y documentación bilingüe breve.
- .NET 8.0.424; build Release con 0 advertencias y 0 errores.
- Asistente Kaggle I2V integrado: alta/verificación, OAuth, CLI 2.2.2 aislada con actualización automática, job privado T4, espera, descarga y limpieza.
- El paso 01 ahora contiene una guía numerada y confirmación explícita; un intento de conectar antes de completarla muestra un aviso y no abre OAuth.
- Las barras de scroll verticales, horizontales e internas comparten un template overlay sin canal sólido, con pulgar redondeado verde apagado y hover/drag discretos; el resto del diseño no cambió.
- TextBox, ComboBox y su popup, ComboBoxItem, CheckBox y Slider comparten ahora superficies cacao mate, borde marrón y foco verde apagado; los campos de solo lectura usan un marrón algo más profundo y ya no aparecen superficies blancas del sistema.
- La navegación principal ahora es `00 GENERAR IMAGEN` → `01 CONVERTIR A VIDEO` → `02 VIDEO` → `03 FONDO / CHROMA` → `04 REGISTRO` → `05 EXPORTACIÓN`.
- El paso 00 elige PNG/JPG/WebP/WIC y convierte localmente la transparencia a chroma verde o azul, incluyendo composición correcta de bordes alfa; Kaggle se abre con ese PNG ya preseleccionado.
- El paso 00 muestra ahora dos visores gemelos sobre damero: `ORIGINAL` y `CHROMA PARA KAGGLE`. El checkmark global usa caja redondeada de 18 px y tilde centrado sin deformación.
- Elegir la imagen dispara chroma verde sin un segundo clic y escribe su ruta en un campo visible del paso 01; pulsar verde o azul regenera el PNG y reemplaza ese campo. Si la preparación faltara, abrir Kaggle la reintenta antes de avanzar.
- El handoff 01 → 02 ya existente se conserva: `USAR ESTE VIDEO` cierra Kaggle, verifica el MP4 y carga su ruta en el campo de video principal.
- El paso 03 suma `Limpiar halo con corte alfa`, activo en 10 %, y `Suavizado del corte`, activo en 4 %. Ambos sliders muestran su valor y actualizan una previsualización sobre damero del cuadro elegido.
- El corte alfa se aplica después del chroma antes de medir límites/alineación y se repite después del escalado para eliminar transparencia débil reintroducida por interpolación. Los originales no se modifican y la exportación registra ambos parámetros.
- Inputs y dropdowns tienen una altura mínima común de 40 px; los botones de filas emparejadas también usan 40 px. Principal y Kaggle tienen minimizar, maximizar/restaurar y cerrar en celdas idénticas de 46 × 40 px.
- La cuenta OAuth está activa y su usuario se detecta automáticamente desde la configuración oficial; ya no se ingresa manualmente ni puede divergir del propietario autenticado.
- El error real `Please select a valid license` se corrigió cambiando el dataset privado transitorio a licencia `other`. Forja detecta el mensaje de creación aunque la CLI devuelva 0 y ya no continúa hacia el 403 secundario.
- La CLI 2.2.2 quedó instalada. Una prueba privada mínima sin imagen ni GPU se creó, llegó a `ready` y se eliminó correctamente; todavía no se consumió cuota GPU.
- Tests locales: 28/28. La CI remota posterior al push no se espera ni monitorea por política.
- Cobertura: líneas 81,14 %, ramas 49,90 %, métodos 76,00 %; umbrales 78/48/72.
- Autoprueba FFmpeg: correcta, incluidos 16 cuadros y todos los artefactos.
- Instalador estándar probado en `%LOCALAPPDATA%`; acceso real, iconos, AppUserModelID, ventana única y cierre normal verificados.
- Las capturas internas 1440×960 y 930×760 verifican los visores nuevos, el checkmark centrado, la explicación del usuario automático y la barra superior fija sin romper el layout.
- Instalación personal actualizada desde el repositorio; acceso real en `Codex Apps`, destino/icono, ventana única, cierre normal y AppUserModelID de proceso `io.github.ldebortoli.ForjaDeCuadros` verificados.
- Gitleaks: cero hallazgos en archivos e historial.
- Secret Scanning, Push Protection, alertas y actualizaciones automáticas de seguridad activos.
- CI usa versiones oficiales de GitHub Actions compatibles con Node 24.
- Por regla global y del proyecto, después de cada push no se espera ni monitorea GitHub Actions salvo pedido explícito; se entrega con validación local y la CI puede quedar pendiente.
- Por instrucción del usuario, las próximas entregas de Forja se publican directamente en `main`, sin PR ni rama auxiliar salvo pedido explícito.

## Próximos pasos

1. Abrir Forja instalada, elegir el PNG en el paso 00 y confirmar los visores original/chroma.
2. Abrir Kaggle I2V; la cuenta ya está conectada y el usuario debe aparecer automáticamente. Pulsar `VERIFICAR` es opcional como comprobación visible.
3. Pulsar `SINCRONIZAR Y GENERAR` para la primera prueba que sí consume cuota GPU y esperar el MP4.
4. Confirmar que el MP4 vuelve al paso 02 y sirve para extraer candidatos; elegir uno y ajustar el corte alfa hasta perder el halo sin cortar detalles, compensando serrucho con suavizado.
5. Preparar Linux/releases sólo cuando el usuario lo pida después de esa prueba real.

## Riesgos

- No publicar `bin/`, `obj/`, `coverage/`, videos, reportes temporales ni datos del usuario.
- La autoprueba FFmpeg de CI es manual para evitar consumo recurrente de cuota.
- El primer job Kaggle puede esperar cola y descargar dependencias/pesos; no afirmar validación GPU real hasta que se ejecute con la cuenta del usuario.
- Computer Use contra Explorer y Forja WPF falló en agosto de 2026 con `Interfaz no compatible (0x80004002)` y geometría no disponible. Fallback validado: captura interna de la app, AppUserModelID y propiedades del acceso. No reintentar antes de septiembre salvo cambio de versión/configuración.
