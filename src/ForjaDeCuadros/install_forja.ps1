param(
    [switch]$SkipBuild,
    [switch]$SelfContained,
    [switch]$CodexApps
)

$ErrorActionPreference = 'Stop'
$toolRoot = (Resolve-Path -LiteralPath $PSScriptRoot).Path
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $toolRoot '..\..')).Path
$projectFile = Join-Path $toolRoot 'ForjaDeCuadros.csproj'
$publishRoot = Join-Path $toolRoot 'bin\Publish\win-x64'

if ($CodexApps) {
    $documents = [Environment]::GetFolderPath([Environment+SpecialFolder]::MyDocuments)
    $codexRoot = Join-Path $documents 'Codex'
    $deployRoot = Join-Path $codexRoot 'AppsData\Forja de Cuadros'
    $shortcutFolder = Join-Path $codexRoot 'CODEX APPS'
} else {
    $deployRoot = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) 'Programs\Forja de Cuadros'
    $shortcutFolder = [Environment]::GetFolderPath([Environment+SpecialFolder]::Programs)
}

$shortcutPath = Join-Path $shortcutFolder 'Forja de Cuadros.lnk'
$executablePath = Join-Path $deployRoot 'ForjaDeCuadros.exe'

if (-not $SkipBuild) {
    $publishArguments = @('publish', $projectFile, '-c', 'Release', '-r', 'win-x64', '--self-contained', $SelfContained.ToString().ToLowerInvariant(), '-o', $publishRoot)
    & dotnet @publishArguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish termino con codigo $LASTEXITCODE" }
}

if (-not (Test-Path -LiteralPath (Join-Path $publishRoot 'ForjaDeCuadros.exe'))) {
    throw "No existe la publicacion esperada en $publishRoot"
}

New-Item -ItemType Directory -Path $deployRoot -Force | Out-Null
Get-ChildItem -LiteralPath $publishRoot -File | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $deployRoot $_.Name) -Force
}
Copy-Item -LiteralPath (Join-Path $projectRoot 'assets\branding\forja-de-cuadros\forja-de-cuadros-icon.png') -Destination (Join-Path $deployRoot 'forja-de-cuadros-icon.png') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'assets\branding\forja-de-cuadros\forja-de-cuadros-icon.ico') -Destination (Join-Path $deployRoot 'forja-de-cuadros-icon.ico') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination (Join-Path $deployRoot 'LEEME.md') -Force

New-Item -ItemType Directory -Path $shortcutFolder -Force | Out-Null
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $executablePath
$shortcut.WorkingDirectory = $deployRoot
$shortcut.IconLocation = "$executablePath,0"
$shortcut.Description = 'Forja de Cuadros · video a sprites de 16 cuadros'
$shortcut.WindowStyle = 1
$shortcut.Save()

$verify = $shell.CreateShortcut($shortcutPath)
if ($verify.TargetPath -ne $executablePath) { throw 'El destino del acceso directo no coincide con la aplicacion publicada.' }
if ($verify.IconLocation -notlike "$executablePath*") { throw 'El acceso directo no usa el icono de la aplicacion.' }

Write-Output "DEPLOY=$deployRoot"
Write-Output "EXE=$executablePath"
Write-Output "SHORTCUT=$shortcutPath"
Write-Output "SHORTCUT_TARGET=$($verify.TargetPath)"
Write-Output "SHORTCUT_ICON=$($verify.IconLocation)"
