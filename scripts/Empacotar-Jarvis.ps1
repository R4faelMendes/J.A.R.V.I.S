param([string]$Configuracao = 'Release')
$ErrorActionPreference = 'Stop'
$raizJarvis = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
Push-Location $raizJarvis
try {
    dotnet build src/Jarvis.Mobile/Jarvis.Mobile.csproj -f net8.0-android -c $Configuracao
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar Android.' }
    & (Join-Path $PSScriptRoot 'Test-ApkAndroid.ps1') -Apk "src/Jarvis.Mobile/bin/$Configuracao/net8.0-android/com.companyname.jarvis.mobile-Signed.apk" -Configuracao $Configuracao
    dotnet build src/Jarvis.Mobile/Jarvis.Mobile.csproj -f net8.0-android -c $Configuracao -r android-arm64 -p:AndroidPackageFormats=apk -o artifacts/android-arm64
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar Android ARM64.' }
    & (Join-Path $PSScriptRoot 'Test-ApkAndroid.ps1') -Apk 'artifacts/android-arm64/com.companyname.jarvis.mobile-Signed.apk' -Configuracao $Configuracao
    dotnet build src/Jarvis.Desktop/Jarvis.Desktop.csproj -c $Configuracao
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao compilar Desktop.' }
    $destinoJarvis = Join-Path $raizJarvis 'artifacts/entrega'
    New-Item -ItemType Directory -Path $destinoJarvis -Force | Out-Null
    Copy-Item -LiteralPath "src/Jarvis.Desktop/bin/$Configuracao/net8.0-windows10.0.19041.0/mobile/Jarvis.apk" -Destination (Join-Path $destinoJarvis 'Jarvis.apk') -Force
    Copy-Item -LiteralPath "src/Jarvis.Desktop/bin/$Configuracao/net8.0-windows10.0.19041.0/mobile/Jarvis-arm64.apk" -Destination (Join-Path $destinoJarvis 'Jarvis-arm64.apk') -Force
    Write-Output "APK pronto: $destinoJarvis/Jarvis.apk"
} finally { Pop-Location }
