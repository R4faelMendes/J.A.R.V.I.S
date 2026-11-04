param([string]$Executavel = (Join-Path $PSScriptRoot '../src/Jarvis.Desktop/bin/Release/net8.0-windows10.0.19041.0/Jarvis.Desktop.exe'))
$ErrorActionPreference = 'Stop'
$jarvisExeVpn = (Resolve-Path -LiteralPath $Executavel).Path
if ([IO.Path]::GetFileName($jarvisExeVpn) -ne 'Jarvis.Desktop.exe') { throw 'Selecione o executável Jarvis.Desktop.exe.' }
$regraJarvisVpn = Get-NetFirewallRule -Name 'Jarvis-Tailscale-5189' -ErrorAction SilentlyContinue
if (-not $regraJarvisVpn) {
    New-NetFirewallRule -Name 'Jarvis-Tailscale-5189' -DisplayName 'Jarvis - Tailscale seguro' -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5189 -LocalAddress '100.64.0.0/10' -RemoteAddress '100.64.0.0/10' -Profile Any -Program $jarvisExeVpn | Out-Null
}
Write-Output 'HTTPS 5189 preparado somente entre endereços da rede privada Tailscale. Entre na mesma conta no PC e no Android.'
if (-not (Get-NetFirewallRule -Name 'Jarvis-Tailscale-Download-5188' -ErrorAction SilentlyContinue)) {
    New-NetFirewallRule -Name 'Jarvis-Tailscale-Download-5188' -DisplayName 'Jarvis - baixar APK pela rede privada Tailscale' -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5188 -LocalAddress '100.64.0.0/10' -RemoteAddress '100.64.0.0/10' -Profile Any -Program $jarvisExeVpn | Out-Null
}
Write-Output 'Download do aplicativo e QR de instalação disponíveis pela porta 5188 na mesma rede privada.'
