# forja-de-cuadros - Contexto del proyecto

## Descripción general

Aplicación WPF gratuita para Windows que transforma videos cortos en paquetes de animación raster de 16 cuadros. FFmpeg extrae y codifica medios; el procesamiento de chroma, registro, auditoría y atlas ocurre localmente en C#.

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

## Convenciones estables

- AppUserModelID: `io.github.ldebortoli.ForjaDeCuadros`.
- El instalador estándar usa `%LOCALAPPDATA%\Programs\Forja de Cuadros` y el menú Inicio; `-CodexApps` usa la carpeta personal del usuario.
- Cerrar la UI cancela el árbol FFmpeg activo.
- La barra superior propia y el ajuste al área útil del monitor deben permanecer accesibles en pantallas compactas.
- Actualizar `.codex/` cuando cambie estado, arquitectura o workflow.
