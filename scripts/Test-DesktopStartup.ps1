param(
    [string]$Executavel = (Join-Path $PSScriptRoot '..\src\Jarvis.Desktop\bin\Release\net8.0-windows10.0.19041.0\Jarvis.Desktop.exe'),
    [ValidateRange(1, 120)][int]$TempoLimiteSegundos = 60
)
$ErrorActionPreference = 'Stop'
$Executavel = (Resolve-Path -LiteralPath $Executavel).Path
if (Get-Process Jarvis.Desktop,Jarvis -ErrorAction SilentlyContinue) { throw 'Feche o Jarvis antes de executar o teste de inicialização.' }
$processo = Start-Process -FilePath $Executavel -ArgumentList '--startup' -WindowStyle Hidden -PassThru
try {
    $relogio = [Diagnostics.Stopwatch]::StartNew()
    do {
        Start-Sleep -Milliseconds 500
        $processo.Refresh()
    } while (!$processo.HasExited -and $processo.MainWindowHandle -eq 0 -and $relogio.Elapsed.TotalSeconds -lt $TempoLimiteSegundos)
    if ($processo.HasExited) { throw "O Jarvis terminou durante a abertura: $($processo.ExitCode)." }
    if ($processo.MainWindowHandle -eq 0) { throw 'A janela principal não foi criada.' }
    $tempoAbertura = [Math]::Round($relogio.Elapsed.TotalSeconds, 1)
    # Exercita também a inicialização de serviços adiada por --startup.
    Start-Sleep -Seconds 8
    $processo.Refresh()
    if ($processo.HasExited) { throw "O Jarvis terminou durante a inicialização dos serviços: $($processo.ExitCode)." }
    if (-not $processo.CloseMainWindow()) { throw 'Não foi possível solicitar o fechamento da janela.' }
    if (-not $processo.WaitForExit(8000)) { throw 'O Jarvis não encerrou em oito segundos.' }
    if ($processo.ExitCode -ne 0) { throw "O encerramento falhou: $($processo.ExitCode)." }
    Write-Output "PASSOU: janela em $tempoAbertura s, serviços com --startup e encerramento sem erro."
} finally {
    if (-not $processo.HasExited) { Stop-Process -Id $processo.Id }
    $processo.Dispose()
}
