param([string]$Dotnet = 'dotnet')
$ErrorActionPreference = 'Stop'
$raizJarvis = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
Push-Location $raizJarvis
try {
    $destinoJarvis = Join-Path $raizJarvis 'artifacts/windows'
    & $Dotnet publish src/Jarvis.Desktop/Jarvis.Desktop.csproj -p:PublishProfile=Portable -r win-x64 --self-contained true -o $destinoJarvis --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao publicar o desktop.' }
    $exeJarvis = Join-Path $destinoJarvis 'Jarvis.Desktop.exe'
    if (!(Test-Path -LiteralPath $exeJarvis)) { throw 'Executável não gerado.' }
    Copy-Item -LiteralPath $exeJarvis -Destination (Join-Path $raizJarvis 'Jarvis.exe') -Force
    $releaseJarvis = Join-Path $raizJarvis 'artifacts/release'
    New-Item -ItemType Directory -Path $releaseJarvis -Force | Out-Null
    Copy-Item -LiteralPath $exeJarvis -Destination (Join-Path $releaseJarvis 'Jarvis-Windows-x64.exe') -Force
    Write-Output "Executável pronto: $raizJarvis\Jarvis.exe"
    Write-Output 'Runtime .NET incluído. Os modelos de voz são instalados separadamente.'
} finally { Pop-Location }
