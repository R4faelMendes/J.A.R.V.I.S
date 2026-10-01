# JARVIS — instalador de primeira execução
#
# O que este script garante antes do app abrir pela primeira vez:
#   1. .NET 8 SDK (runtime) — usa o dotnet-install oficial, sem administrador.
#   2. As fontes Chakra Petch e IBM Plex Sans (já embutidas em fonts/, nada a baixar).
#   3. Os modelos de voz locais (Piper pt-BR e Vosk pt-BR pequeno), que são
#      grandes e por isso não vão no repositório: baixa uma vez e guarda em
#      %LOCALAPPDATA%\Jarvis\modelos.
#   4. O certificado de desenvolvimento HTTPS do pareamento com o celular.
#   5. Compila e abre o JARVIS.
#
# Uso normal: duplo clique em install\Jarvis-Setup.cmd
# Uso manual : powershell -ExecutionPolicy Bypass -File install\bootstrap.ps1

[CmdletBinding()]
param(
    [switch]$SemApp,        # só prepara o ambiente, não abre o app
    [switch]$Forcar,        # refaz etapas já concluídas
    [switch]$SemModelos     # útil em PC muito fraco / sem internet
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$Raiz        = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$DadosJarvis = Join-Path $env:LOCALAPPDATA 'Jarvis'
$Modelos     = Join-Path $DadosJarvis 'modelos'
$DotnetLocal = Join-Path $DadosJarvis 'dotnet'
$Log         = Join-Path $DadosJarvis 'instalacao.log'

New-Item -ItemType Directory -Force -Path $DadosJarvis, $Modelos | Out-Null

function Escreva($texto, $cor = 'Cyan') {
    Write-Host ("  {0}" -f $texto) -ForegroundColor $cor
    Add-Content -Path $Log -Value ("[{0:yyyy-MM-dd HH:mm:ss}] {1}" -f (Get-Date), $texto)
}

function Passo($numero, $total, $titulo) {
    Write-Host ''
    Write-Host ("  [{0}/{1}] {2}" -f $numero, $total, $titulo) -ForegroundColor White
}

function Baixe($url, $destino) {
    if ((Test-Path $destino) -and -not $Forcar) { return }
    Escreva ("baixando {0}" -f (Split-Path -Leaf $destino)) 'DarkGray'
    $tmp = "$destino.parcial"
    Invoke-WebRequest -Uri $url -OutFile $tmp -UseBasicParsing
    Move-Item -Force $tmp $destino
}

Write-Host ''
Write-Host '  JARVIS — preparando este computador' -ForegroundColor Cyan
Write-Host '  ------------------------------------------------------------' -ForegroundColor DarkCyan

$total = if ($SemModelos) { 3 } else { 4 }
$n = 0

# ── 1. .NET 8 ─────────────────────────────────────────────────────────────────
$n++
Passo $n $total 'Runtime .NET 8'

$dotnet = $null
$dotnetExe = Get-Command dotnet -ErrorAction SilentlyContinue
if ($dotnetExe) { $dotnet = $dotnetExe.Source }

if (-not $dotnet) {
    $candidato = Join-Path $DotnetLocal 'dotnet.exe'
    if (Test-Path $candidato) { $dotnet = $candidato }
}

if (-not $dotnet) {
    $instalador = Join-Path $DadosJarvis 'dotnet-install.ps1'
    Baixe 'https://dot.net/v1/dotnet-install.ps1' $instalador
    Escreva 'o .NET não está neste PC; instalando só para o usuário (sem administrador)'
    & $instalador -Channel 8.0 -InstallDir $DotnetLocal -NoPath | Out-Null
    $dotnet = Join-Path $DotnetLocal 'dotnet.exe'
}

if (-not (Test-Path $dotnet)) { throw 'Não foi possível preparar o .NET 8.' }

$versao = (& $dotnet --version) 2>$null
Escreva (".NET {0} pronto" -f $versao) 'Green'

# ── 2. Fontes ─────────────────────────────────────────────────────────────────
$n++
Passo $n $total 'Fontes Chakra Petch e IBM Plex Sans'
$fontes = Join-Path $Raiz 'src\Jarvis.Desktop\Recursos\fontes'
if (Test-Path $fontes) {
    Escreva 'já embutidas no app' 'Green'
} else {
    Escreva 'a pasta de fontes não foi encontrada; o app vai usar a fonte do sistema' 'Yellow'
}

# ── 3. Modelos de voz ─────────────────────────────────────────────────────────
if (-not $SemModelos) {
    $n++
    Passo $n $total 'Modelos de voz locais (Piper pt-BR e Vosk pt-BR)'

    # Piper: voz pt-BR de alta qualidade. Modelo + configuração.
    $piperBase = 'https://huggingface.co/rhasspy/piper-voices/resolve/main/pt/pt_BR/faber/medium'
    Baixe "$piperBase/faber-medium.onnx"      (Join-Path $Modelos 'piper-pt-BR.onnx')
    Baixe "$piperBase/faber-medium.onnx.json" (Join-Path $Modelos 'piper-pt-BR.onnx.json')

    # Vosk: reconhecimento leve para a palavra de ativação e comandos curtos.
    Baixe 'https://alphacephei.com/vosk/models/vosk-model-small-pt-0.3.zip' (Join-Path $Modelos 'vosk-pt.zip')
    $vosk = Join-Path $Modelos 'vosk-pt'
    if (-not (Test-Path $vosk)) {
        Expand-Archive -Path (Join-Path $Modelos 'vosk-pt.zip') -DestinationPath $Modelos -Force
        $extraido = Get-ChildItem $Modelos -Directory | Where-Object { $_.Name -like 'vosk-model-small-pt*' } | Select-Object -First 1
        if ($extraido) { Move-Item $extraido.FullName $vosk }
    }
    Escreva 'modelos prontos em %LOCALAPPDATA%\Jarvis\modelos' 'Green'
}

# ── 4. Certificado HTTPS e compilação ─────────────────────────────────────────
$n++
Passo $n $total 'Certificado local e compilação'

& $dotnet dev-certs https --trust 2>$null | Out-Null
Escreva 'certificado de desenvolvimento verificado' 'DarkGray'

$solucao = Join-Path $Raiz 'Jarvis.sln'
if (Test-Path $solucao) {
    Escreva 'compilando…' 'DarkGray'
    & $dotnet build $solucao -c Release --nologo -v quiet | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'A compilação falhou. Rode dotnet build para ver o erro.' }
    Escreva 'compilado' 'Green'
}

Write-Host ''
Write-Host '  Pronto. O JARVIS está instalado neste computador.' -ForegroundColor Green
Write-Host "  Dados, modelos e log: $DadosJarvis" -ForegroundColor DarkGray
Write-Host ''

if (-not $SemApp -and (Test-Path $solucao)) {
    $app = Join-Path $Raiz 'src\Jarvis.Desktop\bin\Release\net8.0-windows\Jarvis.Desktop.exe'
    if (Test-Path $app) { Start-Process $app }
}
