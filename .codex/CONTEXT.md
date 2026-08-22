# forja-de-cuadros - Contexto del proyecto

## Descripción general

Aplicación WPF gratuita para Windows que prepara imágenes transparentes sobre chroma y transforma videos cortos en paquetes de animación raster de 16 cuadros. FFmpeg extrae y codifica medios; la preparación alfa/chroma, el procesamiento, registro, auditoría y atlas ocurren localmente en C#. Como fuente opcional, un asistente Kaggle I2V recibe la imagen preparada y la convierte en un MP4 mediante un trabajo cloud privado.

## Repositorio

- Raíz local: `%USERPROFILE%\Documents\GitHub\forja-de-cuadros`.
- Remoto público: `https://github.com/ldebortoli/forja-de-cuadros`.
- Rama primaria: `main`.
- Licencia: MIT.
- GitHub Secret Scanning, Push Protection, alertas de vulnerabilidad y actualizaciones automáticas de seguridad están activos.
- No versionar binarios, videos, exportaciones, logs, secretos ni rutas personales.

## Stack y estructura

- .NET 8, C#, WPF/WinForms interop, Windows x64.
- `src/ForjaDeCuadros/`: aplicación, instalador y validadores.
- `KaggleWindow`, `KaggleCliService` y `KaggleModels`: onboarding, OAuth oficial, generación del job privado y recuperación del MP4.
- `tests/ForjaDeCuadros.Tests/`: tests xUnit del núcleo de procesamiento.
- `assets/branding/forja-de-cuadros/`: icono común de app, ventana y acceso directo.
- `docs/`: workflow y capturas.
- `.github/workflows/ci.yml`: build, tests y cobertura en push/PR con acciones basadas en Node 24.
- `.github/workflows/integration.yml`: autoprueba FFmpeg manual.

## Comandos verificados

```powershell
dotnet restore ForjaDeCuadros.sln
dotnet build ForjaDeCuadros.sln -c Release --no-restore
dotnet test ForjaDeCuadros.sln -c Release --no-build --no-restore
dotnet test tests\ForjaDeCuadros.Tests\ForjaDeCuadros.Tests.csproj -c Release --no-build --no-restore /p:CollectCoverage=true
powershell -NoProfile -ExecutionPolicy Bypass -File src\ForjaDeCuadros\install_forja.ps1
```

La autoprueba completa se ejecuta con `ForjaDeCuadros.exe --self-test <reporte.json>` mediante `Start-Process -Wait`, porque el ejecutable es `WinExe`.

Kaggle CLI 2.2.2 queda en `%LOCALAPPDATA%\ForjaDeCuadros\Kaggle\cli`; requiere Python 3.11+. Forja actualiza automáticamente versiones anteriores, obtiene el usuario desde `kaggle config view` después de OAuth, consulta porcentaje/horas mediante `kaggle quota --csv` y usa `other` como licencia válida del dataset privado transitorio. Los jobs usan LTX-Video 2B 0.9.8 distilled fijado al commit `4b2d053057623ddd4d0a1d3e9cd28890e9ef487f`, solicitan `NvidiaTeslaT4` y adaptan el source fijado para offload secuencial, dispositivos coherentes y VAE temporal por bloques causales solapados.

## Convenciones estables

- AppUserModelID: `io.github.ldebortoli.ForjaDeCuadros`.
- El instalador estándar usa `%LOCALAPPDATA%\Programs\Forja de Cuadros` y el menú Inicio; `-CodexApps` usa la carpeta personal del usuario.
- Cerrar la UI cancela el árbol FFmpeg activo.
- La barra superior propia y el ajuste al área útil del monitor deben permanecer accesibles en pantallas compactas.
- Todas las superficies WPF usan una barra de scroll global tipo overlay: sin canal sólido, pulgar redondeado verde apagado y estados hover/drag sobrios; mantenerla consistente en vertical, horizontal y campos internos.
- Los controles de entrada WPF usan superficies cacao mate, borde marrón y foco verde apagado: campos editables/solo lectura, dropdowns y su popup, checks y sliders deben conservar esta familia sin fondos blancos del sistema.
- Los checks usan una caja redondeada de 18 px y un tilde no estirado, centrado y de trazo redondeado; no volver a delegar su geometría al estiramiento automático de WPF.
- TextBox y ComboBox usan una altura mínima común de 40 px; los botones que comparten fila deben usar esa misma altura. Las barras superiores principal y Kaggle distribuyen minimizar, maximizar/restaurar y cerrar en tres celdas idénticas de 46 × 40 px.
- Después de un push no se espera, monitorea ni sondea la CI remota salvo pedido explícito del usuario; las validaciones locales siguen siendo obligatorias.
- Publicar los cambios mantenidos directamente en `main`; no crear ramas auxiliares ni pull requests salvo que el usuario lo pida explícitamente.
- Kaggle es estrictamente opcional: input y kernel privados, OAuth manejado por la CLI oficial, limpieza remota activada por defecto y temporales locales eliminados después de una descarga correcta.
- La generación Kaggle/LTX quedó técnicamente validada pero artísticamente descartada para el personaje actual por deriva severa de identidad/anatomía y ruido de chroma. No consumir más cuota GPU ni recomendar este flujo para producción salvo que el usuario lo pida explícitamente o se cambie de modelo.
- El origen I2V puede ser cualquier aplicación externa: el usuario eligió generar los clips fuera de Forja y cargarlos directamente en `02 VIDEO`; el procesamiento posterior no depende de Kaggle.
- El flujo visible comienza en `00 GENERAR IMAGEN`: elegir el archivo prepara chroma verde automáticamente y rellena la ruta visible de `01 CONVERTIR A VIDEO`; cambiar a azul reemplaza esa ruta. El MP4 aceptado en Kaggle rellena automáticamente `02 VIDEO`.
- El paso 00 muestra dos visores iguales sobre damero, `ORIGINAL` y `CHROMA PARA KAGGLE`; se actualizan al elegir la imagen y al regenerar verde/azul.
- La limpieza posterior al chroma usa corte alfa activado por defecto en 10 % y suavizado en 4 %. Se aplica antes de calcular límites/alineación y después del remuestreo; la UI ofrece previsualización sobre damero y `--capture-alpha` permite revisar el panel completo.
- Computer Use contra Explorer y Forja WPF produjo `Interfaz no compatible (0x80004002)` en agosto de 2026. No reintentar captura/coordenadas antes de septiembre de 2026 salvo cambio de versión; usar las capturas internas, la identidad de proceso y las propiedades COM del acceso como fallback.
- Actualizar `.codex/` cuando cambie estado, arquitectura o workflow.
