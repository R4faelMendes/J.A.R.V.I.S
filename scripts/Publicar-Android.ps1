param([string]$Dotnet = 'dotnet')
$ErrorActionPreference = 'Stop'
foreach ($nome in @('JARVIS_ANDROID_KEYSTORE', 'JARVIS_ANDROID_PASSWORD')) {
    if (![Environment]::GetEnvironmentVariable($nome)) { throw "Defina $nome para assinar o APK com sua chave de distribuição." }
}
$raizJarvis = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
Push-Location $raizJarvis
try {
    & $Dotnet publish src/Jarvis.Mobile/Jarvis.Mobile.csproj -f net8.0-android -c Release `
        -p:AndroidPackageFormats=apk -p:AndroidKeyStore=true `
        "-p:AndroidSigningKeyStore=$env:JARVIS_ANDROID_KEYSTORE" -p:AndroidSigningKeyAlias=jarvis `
        -p:AndroidSigningStorePass=env:JARVIS_ANDROID_PASSWORD -p:AndroidSigningKeyPass=env:JARVIS_ANDROID_PASSWORD --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Falha ao publicar Android.' }
    $apkJarvis = Join-Path $raizJarvis 'src/Jarvis.Mobile/bin/Release/net8.0-android/publish/com.companyname.jarvis.mobile-Signed.apk'
    & (Join-Path $PSScriptRoot 'Test-ApkAndroid.ps1') -Apk $apkJarvis
    $releaseJarvis = Join-Path $raizJarvis 'artifacts/release'
    New-Item -ItemType Directory -Path $releaseJarvis -Force | Out-Null
    Copy-Item -LiteralPath $apkJarvis -Destination (Join-Path $releaseJarvis 'Jarvis-Android.apk') -Force
    Write-Output "APK assinado pronto: $releaseJarvis\Jarvis-Android.apk"
} finally { Pop-Location }
