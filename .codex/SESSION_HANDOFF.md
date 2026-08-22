# Session handoff

## Objetivo general

Mantener Forja de Cuadros como herramienta Windows gratuita y abierta, separada del proyecto de juego.

## Tarea actual

El workflow Kaggle T4 real, el diagnóstico visible, `VERIFICAR` y la cuota GPU quedaron corregidos, validados, instalados y publicados. La revisión artística descartó Kaggle/LTX para el personaje actual y el usuario decidió generar el video con otra aplicación y cargarlo en `02 VIDEO`. Linux/releases continúa diferido.

## Estado actual

- Repositorio público: `https://github.com/ldebortoli/forja-de-cuadros`.
- Rama primaria: `main`.
- La entrega funcional quedó publicada directamente en `main` mediante `c54ad63`. No se creó PR ni se esperó o monitoreó la CI.
- Entrega inicial publicada con licencia MIT y documentación bilingüe breve.
- .NET 8.0.424; build Release con 0 advertencias y 0 errores.
- Asistente Kaggle I2V integrado: alta/verificación, OAuth, CLI 2.2.2 aislada con actualización automática, cuota oficial, job privado T4, espera, diagnóstico, descarga y limpieza.
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
- La primera generación GPU reveló que el source LTX fijado cargaba transformer, VAE, T5, Florence y Llama de forma incompatible con una T4. El kernel nuevo desactiva el mejorador opcional, aplica offload secuencial y corrige los dispositivos del condicionamiento, reescalador y VAE.
- La decodificación VAE usa bloques temporales causales solapados de modo que 13 latentes reconstruyen y recortan exactamente 97 cuadros sin intentar reservar otros 16 GB de VRAM. El script aborta si el source fijado cambia o devuelve menos cuadros.
- La prueba real terminó en Kaggle T4 y devolvió H.264 512×512, 30 FPS, 3,23 s y 97/97 cuadros. El video validado quedó en `%USERPROFILE%\Videos\Forja de Cuadros\Kaggle`; el kernel, dataset y workspace temporal se eliminaron.
- La revisión del cuadro intermedio mostró deriva severa de identidad/anatomía y ruido de chroma; el usuario descartó este modelo para animaciones reales. No ejecutar nuevas generaciones Kaggle salvo pedido explícito o cambio de modelo.
- El flujo de producción elegido usa una aplicación I2V externa y entrega su MP4 en `02 VIDEO`; extracción, chroma, corte alfa, registro y atlas continúan normalmente sin Kaggle.
- `VERIFICAR` muestra ahora un panel persistente y un modal inequívoco. Los fallos descargan el log mediante Python UTF-8 y traducen memoria, cuota, dispositivos, reescalado y decodificación conocidos.
- El panel muestra cuota GPU semanal mediante `kaggle quota --csv`: porcentaje restante, horas restantes/usadas y fecha de reinicio. Después de las pruebas reales informó 96,2 %, 28,86/30 h y reinicio 29/08/2026.
- Tests locales: 36/36. La CI remota posterior al push no se espera ni monitorea por política.
- Cobertura: líneas 85,57 %, ramas 51,40 %, métodos 77,35 %; umbrales 78/48/72.
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

1. Cargar en `02 VIDEO` el próximo MP4 producido con la aplicación I2V externa que elija el usuario.
2. Investigar o integrar otro proveedor/modelo I2V sólo cuando el usuario lo pida; no consumir más cuota Kaggle por defecto.
3. Mantener Kaggle como integración opcional pública o quitar/ocultar su entrada si el usuario lo solicita explícitamente.
4. Preparar Linux/releases sólo cuando el usuario lo pida explícitamente.

## Riesgos

- No publicar `bin/`, `obj/`, `coverage/`, videos, reportes temporales ni datos del usuario.
- La autoprueba FFmpeg de CI es manual para evitar consumo recurrente de cuota.
- El kernel adapta archivos exactos del commit LTX fijado y aborta si sus marcadores cambian; conservar el pin o actualizar las adaptaciones y repetir una prueba GPU real al migrarlo.
- La decodificación causal por bloques puede mostrar costuras temporales propias del modelo; revisar visualmente el loop antes de extraer cuadros.
- Computer Use contra Explorer y Forja WPF falló en agosto de 2026 con `Interfaz no compatible (0x80004002)` y geometría no disponible. Fallback validado: captura interna de la app, AppUserModelID y propiedades del acceso. No reintentar antes de septiembre salvo cambio de versión/configuración.
