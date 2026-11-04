param(
    [Parameter(Mandatory = $true)][string]$Apk,
    [string]$Configuracao = 'Release'
)
$ErrorActionPreference = 'Stop'
$raizTesteApk = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$arquivoTesteApk = (Resolve-Path -LiteralPath $Apk).Path
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zipTesteApk = [IO.Compression.ZipFile]::OpenRead($arquivoTesteApk)
try {
    $entradasTesteApk = @($zipTesteApk.Entries | ForEach-Object FullName)
    $abisTesteApk = @($entradasTesteApk | ForEach-Object { if ($_ -match '^lib/([^/]+)/libmonodroid\.so$') { $Matches[1] } })
    if ($abisTesteApk.Count -eq 0) { throw 'APK sem runtime Android.' }
    foreach ($abiTesteApk in $abisTesteApk) {
        $nomeSkiaApk = "lib/$abiTesteApk/libSkiaSharp.so"
        $entradaSkiaApk = $zipTesteApk.GetEntry($nomeSkiaApk)
        if ($null -eq $entradaSkiaApk -or $entradaSkiaApk.Length -lt 100000) { throw "APK inválido: biblioteca gráfica ausente para $abiTesteApk ($nomeSkiaApk). O app pode fechar ao abrir o reator." }
    }
    $iconesTesteApk = @($entradasTesteApk | Where-Object { $_ -match '(^|/)appicon(?:_round|_foreground|_background)?\.(png|xml)$' })
    if ($iconesTesteApk.Count -lt 2) { throw 'Ícones do launcher/adaptativo ausentes no APK.' }
} finally { $zipTesteApk.Dispose() }
$caminhoAssetsTesteApk = Join-Path $raizTesteApk 'src/Jarvis.Mobile/obj/project.assets.json'
$assetsTesteApk = Get-Content -LiteralPath $caminhoAssetsTesteApk -Raw | ConvertFrom-Json
$pacotesTesteApk = @($assetsTesteApk.libraries.PSObject.Properties | ForEach-Object Name)
foreach ($pacoteTesteApk in @('SkiaSharp/2.88.9', 'SkiaSharp.Views.Maui.Controls/2.88.9', 'SkiaSharp.NativeAssets.Android/2.88.9')) {
    if ($pacoteTesteApk -notin $pacotesTesteApk) { throw "Dependências do mobile incompatíveis: falta $pacoteTesteApk." }
}
if ($pacotesTesteApk | Where-Object { $_ -like 'SkiaSharp/4.*' -or $_ -like 'ZXing.Net.Bindings.SkiaSharp/*' }) { throw 'Dependência do leitor QR voltou a alterar a versão gráfica do mobile.' }
$manifestoTesteApk = Join-Path $raizTesteApk "src/Jarvis.Mobile/obj/$Configuracao/net8.0-android/AndroidManifest.xml"
[xml]$xmlTesteApk = Get-Content -LiteralPath $manifestoTesteApk -Raw
if ($xmlTesteApk.manifest.application.GetAttribute('icon', 'http://schemas.android.com/apk/res/android') -ne '@mipmap/appicon') { throw 'O launcher não está configurado para o ícone Jarvis.' }
if ($Configuracao -eq 'Release' -and $xmlTesteApk.manifest.application.GetAttribute('debuggable', 'http://schemas.android.com/apk/res/android') -eq 'true') { throw 'APK de entrega não pode permitir depuração.' }
$nsApkJarvis = 'http://schemas.android.com/apk/res/android'
if ([version]$xmlTesteApk.manifest.GetAttribute('versionName', $nsApkJarvis) -ge [version]'1.6.0') {
    $permissoesApkJarvis = @($xmlTesteApk.manifest.'uses-permission' | ForEach-Object { $_.GetAttribute('name', $nsApkJarvis) })
    foreach ($permissaoApkJarvis in @('POST_NOTIFICATIONS', 'RECEIVE_BOOT_COMPLETED', 'SCHEDULE_EXACT_ALARM', 'FOREGROUND_SERVICE_MEDIA_PLAYBACK')) {
        if ('android.permission.' + $permissaoApkJarvis -notin $permissoesApkJarvis) { throw "Falta permissão Android para $permissaoApkJarvis." }
    }
    if (-not ($xmlTesteApk.manifest.application.service | Where-Object { $_.GetAttribute('name', $nsApkJarvis) -like '*.SpotifyRemotoService' -and $_.GetAttribute('foregroundServiceType', $nsApkJarvis) -eq 'mediaPlayback' -and $_.GetAttribute('exported', $nsApkJarvis) -eq 'false' })) { throw 'Serviço de mídia remota inválido no APK.' }
    foreach ($receiverApkJarvis in @('AlertaAgendaReceiver', 'RestaurarAgendaReceiver')) {
        if (-not ($xmlTesteApk.manifest.application.receiver | Where-Object { $_.GetAttribute('name', $nsApkJarvis) -like "*.$receiverApkJarvis" })) { throw "Falta receiver $receiverApkJarvis." }
    }
}
if ([version]$xmlTesteApk.manifest.GetAttribute('versionName', $nsApkJarvis) -ge [version]'1.7.0') {
    foreach ($permissaoApkJarvis in @('FOREGROUND_SERVICE_CONNECTED_DEVICE', 'FOREGROUND_SERVICE_MEDIA_PROJECTION', 'CHANGE_NETWORK_STATE')) {
        if ('android.permission.' + $permissaoApkJarvis -notin $permissoesApkJarvis) { throw "Falta permissão do aparelho: $permissaoApkJarvis." }
    }
    foreach ($tipoApkAparelho in @(@('AparelhoService', 'connectedDevice'), @('EspelhoCelularService', 'mediaProjection'))) {
        if (-not ($xmlTesteApk.manifest.application.service | Where-Object { $_.GetAttribute('name', $nsApkJarvis) -like "*.$($tipoApkAparelho[0])" -and $_.GetAttribute('foregroundServiceType', $nsApkJarvis) -eq $tipoApkAparelho[1] -and $_.GetAttribute('exported', $nsApkJarvis) -eq 'false' })) { throw "Serviço de aparelho inválido: $($tipoApkAparelho[0])." }
    }
}
if ([version]$xmlTesteApk.manifest.GetAttribute('versionName', $nsApkJarvis) -ge [version]'1.9.0' -and [version]$xmlTesteApk.manifest.GetAttribute('versionName', $nsApkJarvis) -lt [version]'2.0.1') {
    if (-not ($xmlTesteApk.manifest.application.service | Where-Object { $_.GetAttribute('name', $nsApkJarvis) -like '*.ControleCelularAndroid' -and $_.GetAttribute('permission', $nsApkJarvis) -eq 'android.permission.BIND_ACCESSIBILITY_SERVICE' })) { throw 'Serviço de controle do Android sem a proteção BIND_ACCESSIBILITY_SERVICE.' }
    if ($xmlTesteApk.manifest.queries.package | Where-Object { $null -ne $_ -and $_.GetAttribute('name', $nsApkJarvis) -eq 'com.tailscale.ipn' }) { throw 'Dependência de descoberta do Tailscale ainda presente.' }
}
if ([version]$xmlTesteApk.manifest.GetAttribute('versionName', $nsApkJarvis) -ge [version]'2.0.0') {
    if ('android.permission.RECORD_AUDIO' -notin $permissoesApkJarvis) { throw 'Falta permissão para o microfone interno do Jarvis.' }
}
[pscustomobject]@{
    Apk = $arquivoTesteApk
    Versao = $xmlTesteApk.manifest.GetAttribute('versionName', 'http://schemas.android.com/apk/res/android')
    Arquiteturas = $abisTesteApk -join ', '
    SkiaNativoCompleto = $true
    IconesNoPacote = $iconesTesteApk.Count
    Sha256 = (Get-FileHash -LiteralPath $arquivoTesteApk -Algorithm SHA256).Hash
}

if ([version]$xmlTesteApk.manifest.GetAttribute('versionName', $nsApkJarvis) -ge [version]'2.0.1') {
    if ($xmlTesteApk.OuterXml -match 'BIND_ACCESSIBILITY_SERVICE|accessibilityservice|NotificationListenerService|READ_SMS|RECEIVE_SMS|BIND_DEVICE_ADMIN') { throw 'O instalador padrão voltou a declarar acesso sensível a outros aplicativos.' }
    if ($entradasTesteApk | Where-Object { $_ -match 'jarvis_acessibilidade|controle_jarvis' }) { throw 'Recursos do serviço sensível ainda estão no APK.' }
}
