param([string]$Executavel = (Join-Path $PSScriptRoot '..\src\Jarvis.Desktop\bin\Release\net8.0-windows10.0.19041.0\Jarvis.Desktop.exe'))
$ErrorActionPreference = 'Stop'
$Executavel = (Resolve-Path -LiteralPath $Executavel).Path
if (Get-Process Jarvis.Desktop -ErrorAction SilentlyContinue) { throw 'Feche o Jarvis antes de executar o teste de inicialização.' }
$processo = Start-Process -FilePath $Executavel -ArgumentList '--startup' -WindowStyle Hidden -PassThru
try {
    Start-Sleep -Seconds 15
    $processo.Refresh()
    if ($processo.HasExited) { throw "O Jarvis terminou durante a abertura: $($processo.ExitCode)." }
    if ($processo.MainWindowHandle -eq 0) { throw 'A janela principal não foi criada.' }
    if (-not $processo.CloseMainWindow()) { throw 'Não foi possível solicitar o fechamento da janela.' }
    if (-not $processo.WaitForExit(8000)) { throw 'O Jarvis não encerrou em oito segundos.' }
    if ($processo.ExitCode -ne 0) { throw "O encerramento falhou: $($processo.ExitCode)." }
    Write-Output 'PASSOU: abertura com --startup, janela principal e encerramento sem erro.'
} finally {
    if (-not $processo.HasExited) { Stop-Process -Id $processo.Id }
    $processo.Dispose()
}
