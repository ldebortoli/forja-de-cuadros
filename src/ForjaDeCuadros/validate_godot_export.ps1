param(
    [Parameter(Mandatory = $true)][string]$ExportFolder,
    [string]$GodotExecutable = ''
)

$ErrorActionPreference = 'Stop'
$resolvedExport = (Resolve-Path -LiteralPath $ExportFolder).Path
$atlas = Join-Path $resolvedExport 'autoprueba_atlas.png'
$spriteFrames = Join-Path $resolvedExport 'autoprueba_spriteframes.tres'
if (-not (Test-Path -LiteralPath $atlas) -or -not (Test-Path -LiteralPath $spriteFrames)) {
    throw 'El paquete de autoprueba no contiene atlas y SpriteFrames esperados.'
}
if ([string]::IsNullOrWhiteSpace($GodotExecutable)) {
    $command = Get-Command godot, godot4 -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($command) {
        $GodotExecutable = $command.Source
    }
    else {
        $wingetPackage = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) 'Microsoft\WinGet\Packages\GodotEngine.GodotEngine_Microsoft.Winget.Source_8wekyb3d8bbwe'
        if (Test-Path -LiteralPath $wingetPackage) {
            $GodotExecutable = Get-ChildItem -LiteralPath $wingetPackage -File -Filter 'Godot*_console.exe' -ErrorAction SilentlyContinue | Sort-Object Name -Descending | Select-Object -First 1 -ExpandProperty FullName
        }
    }
}
if (-not (Test-Path -LiteralPath $GodotExecutable)) { throw "No se encontro Godot en $GodotExecutable" }

$validationRoot = Join-Path ([IO.Path]::GetTempPath()) ('ForjaGodotValidation-' + [Guid]::NewGuid().ToString('N'))
$textureFolder = Join-Path $validationRoot 'assets\sprites\generated'
[IO.Directory]::CreateDirectory($textureFolder) | Out-Null
[IO.File]::Copy($atlas, (Join-Path $textureFolder 'autoprueba_atlas.png'), $true)
[IO.File]::Copy($spriteFrames, (Join-Path $validationRoot 'autoprueba_spriteframes.tres'), $true)
$projectConfig = @'
[application]
config/name="ForjaExportValidation"
[rendering]
renderer/rendering_method="gl_compatibility"
'@
[IO.File]::WriteAllText((Join-Path $validationRoot 'project.godot'), $projectConfig)
[IO.File]::WriteAllText((Join-Path $validationRoot 'validate.gd'), @'
extends SceneTree

func _initialize() -> void:
    var frames := load("res://autoprueba_spriteframes.tres") as SpriteFrames
    if frames == null:
        push_error("FORJA_TRES_LOAD_FAILED")
        quit(1)
        return
    if not frames.has_animation(&"autoprueba"):
        push_error("FORJA_ANIMATION_MISSING")
        quit(2)
        return
    if frames.get_frame_count(&"autoprueba") != 16:
        push_error("FORJA_FRAME_COUNT=" + str(frames.get_frame_count(&"autoprueba")))
        quit(3)
        return
    for index in range(16):
        var texture := frames.get_frame_texture(&"autoprueba", index) as AtlasTexture
        if texture == null or texture.region.size != Vector2(256, 256):
            push_error("FORJA_FRAME_REGION_INVALID=" + str(index))
            quit(4)
            return
    print("FORJA_GODOT_EXPORT=PASS")
    quit(0)
'@)

try {
    & $GodotExecutable --headless --editor --path $validationRoot --quit
    if ($LASTEXITCODE -ne 0) { throw "La importacion temporal de Godot termino con codigo $LASTEXITCODE" }
    & $GodotExecutable --headless --path $validationRoot --script res://validate.gd
    if ($LASTEXITCODE -ne 0) { throw "La validacion SpriteFrames termino con codigo $LASTEXITCODE" }
    Write-Output 'GODOT_EXPORT_VALIDATION=PASS'
}
finally {
    $fullValidation = [IO.Path]::GetFullPath($validationRoot)
    $expectedPrefix = [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetTempPath()) 'ForjaGodotValidation-'))
    if ($fullValidation.StartsWith($expectedPrefix, [StringComparison]::OrdinalIgnoreCase) -and [IO.Directory]::Exists($fullValidation)) {
        [IO.Directory]::Delete($fullValidation, $true)
    }
}
