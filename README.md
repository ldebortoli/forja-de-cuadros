# Forja de Cuadros

Aplicación gratuita y local para convertir un video corto en una animación raster de 16 cuadros lista para revisar e integrar en Godot.

![Forja de Cuadros en una pantalla compacta](docs/images/forja-small-monitor.png)

La herramienta recibe MP4, MOV, WebM o GIF, extrae fotogramas con FFmpeg y permite seleccionar, limpiar, alinear, auditar y exportar la animación. No genera video con IA, no depende de ComfyUI y no envía tus archivos a internet.

> **English:** A free, local Windows tool that turns a short video into a reviewed 16-frame raster animation package for Godot. The UI is currently in Spanish.

## Qué resuelve

- Extrae candidatos de un tramo preciso del video.
- Distribuye automáticamente una selección inicial de 16 cuadros.
- Quita fondos verdes o azules con tolerancia, suavizado, despill, erosión de halo y limpieza de islas.
- Mantiene una escala común y registra raíz/suelo según el tipo de movimiento.
- Detecta cuadros vacíos, duplicados, recortes, deriva de altura/suelo/raíz y una mala costura del loop.
- Exporta PNG individuales, atlas 4×4 u horizontal, GIF, revisión HTML, metadata JSON y `SpriteFrames.tres`.
- Conserva cada exportación en una carpeta nueva: no pisa animaciones previas.
- Incluye un asistente Kaggle I2V para convertir una imagen en MP4 mediante un trabajo privado con GPU T4 y cargar el resultado directamente en la mesa de selección.

## Requisitos

- Windows 10 u 11, x64.
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) para compilar. Una publicación dependiente del framework requiere además el .NET 8 Desktop Runtime.
- [FFmpeg](https://ffmpeg.org/) y `ffprobe` disponibles en `PATH`. En Windows también se detecta la instalación de WinGet:

```powershell
winget install --id Gyan.FFmpeg --exact
```

## Inicio rápido

```powershell
git clone https://github.com/ldebortoli/forja-de-cuadros.git
cd forja-de-cuadros
dotnet build ForjaDeCuadros.sln -c Release
dotnet run --project src\ForjaDeCuadros\ForjaDeCuadros.csproj -c Release
```

Para instalarla en el menú Inicio con su icono propio:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File src\ForjaDeCuadros\install_forja.ps1
```

Usá `-SelfContained` para incluir el runtime de .NET en la instalación, o `-CodexApps` si querés instalarla en tu carpeta personal `Codex Apps`.

## Workflow recomendado

1. Generá un clip de 2–4 segundos desde una imagen aprobada con la herramienta I2V que prefieras.
2. Pedí cámara fija, personaje completo, una sola acción y fondo verde o azul uniforme.
3. Abrí el clip en Forja, definí inicio/final y extraé candidatos.
4. Elegí exactamente 16 cuadros; `AUTO 16` distribuye la selección sobre todo el tramo.
5. Tomá el color del fondo y elegí el registro:
   - **Suelo + raíz fijos:** loops in-place.
   - **Suelo fijo, conservar avance:** locomoción a través del canvas.
   - **Cámara fija:** saltos, caídas o dash con movimiento vertical.
6. Procesá y revisá el GIF, los bordes, las huellas únicas y la costura `16 → 01`.
7. Exportá el paquete e integrá el atlas aprobado a la ruta declarada para Godot.

### Generar el clip desde la propia aplicación

El botón `KAGGLE I2V` de la sección **Fuente** abre un asistente integrado. Prepara la CLI oficial en un entorno aislado, conecta la cuenta mediante OAuth en el navegador, sube la imagen como dataset privado temporal, ejecuta LTX-Video 2B en una GPU T4, espera el resultado y recupera el MP4. Forja nunca pide la contraseña ni escribe tokens dentro del proyecto.

La cuenta debe tener correo y teléfono verificados para acceder a GPU. La disponibilidad es compartida, puede haber cola y la cuota semanal varía. Consultá el [instructivo completo de Kaggle](docs/kaggle.md).

La auditoría automática encuentra problemas mecánicos, pero no reemplaza la revisión de anatomía, dirección de pies, ropa, pelo o equipo rígido.

## Desarrollo, tests y cobertura

Suite rápida y determinista:

```powershell
dotnet test ForjaDeCuadros.sln -c Release --no-restore
```

Cobertura con umbrales de regresión para el núcleo de procesamiento (líneas 78 %, ramas 48 % y métodos 72 %):

```powershell
dotnet test tests\ForjaDeCuadros.Tests\ForjaDeCuadros.Tests.csproj -c Release --no-restore /p:CollectCoverage=true
```

Autoprueba completa con FFmpeg (más lenta):

```powershell
dotnet build src\ForjaDeCuadros\ForjaDeCuadros.csproj -c Release
$report = Join-Path $PWD 'artifacts\self-test.json'
$process = Start-Process -FilePath 'src\ForjaDeCuadros\bin\Release\net8.0-windows\ForjaDeCuadros.exe' -ArgumentList @('--self-test', $report) -WindowStyle Hidden -Wait -PassThru
exit $process.ExitCode
```

CI ejecuta build, tests y cobertura en cada push y pull request. La autoprueba con FFmpeg queda como workflow manual para cuidar los minutos gratuitos.

Coverlet mide líneas, ramas y métodos; no expone una métrica de *statements* separada, por lo que líneas es el control equivalente para sentencias ejecutables. La medición local actual es 80,61 % de líneas, 48,98 % de ramas y 73,72 % de métodos.

## Privacidad y alcance

- La selección tradicional de cuadros y la exportación ocurren enteramente en tu PC.
- El flujo local no requiere cuentas, telemetría, API keys ni servicios en la nube.
- El procesamiento tradicional sigue siendo enteramente local. `KAGGLE I2V` es opcional y sí envía la imagen y el prompt a Kaggle cuando el usuario pulsa `SINCRONIZAR Y GENERAR`.
- Los trabajos Kaggle creados por Forja son privados. La limpieza remota posterior a la descarga está activada por defecto.
- Los logs de fallos, si existen, quedan en `%LOCALAPPDATA%\ForjaDeCuadros\Logs`.
- Los binarios, videos, exportaciones, logs y contenido del usuario están excluidos del repositorio.

## Licencia

[MIT](LICENSE). Podés usar, modificar y compartir la herramienta, incluso en proyectos comerciales.
