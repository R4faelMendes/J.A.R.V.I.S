$ErrorActionPreference = 'Stop'
$raizJarvis = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$destinoSite = Join-Path $raizJarvis 'artifacts/site'
New-Item -ItemType Directory -Path $destinoSite -Force | Out-Null
Copy-Item -Path (Join-Path $raizJarvis 'site/*') -Destination $destinoSite -Recurse -Force
$fontesSite = Join-Path $destinoSite 'fonts'
New-Item -ItemType Directory -Path $fontesSite -Force | Out-Null
foreach ($fonte in @('ChakraPetch-SemiBold.ttf', 'IBMPlexSans-Regular.ttf')) {
    Copy-Item -LiteralPath (Join-Path $raizJarvis "src/Jarvis.Mobile/Resources/Fonts/$fonte") -Destination $fontesSite -Force
}
Copy-Item -LiteralPath (Join-Path $raizJarvis 'src/Jarvis.Desktop/Assets/jarvis.ico') -Destination $destinoSite -Force
# Preserva os avisos de licença junto das fontes distribuídas.
Get-ChildItem -LiteralPath (Join-Path $raizJarvis 'src/Jarvis.Desktop/Assets/Fonts') -File |
    Where-Object { $_.Extension -ne '.ttf' } | Copy-Item -Destination $fontesSite -Force
$historicoSite = @(git -C $raizJarvis log -5 '--format=%H%x09%cI%x09%s' | ForEach-Object {
    $campos = $_ -split "`t", 3
    [ordered]@{ sha = $campos[0]; date = $campos[1]; subject = $campos[2] }
})
if ($LASTEXITCODE -ne 0) { throw 'Falha ao consultar o histórico Git.' }
@{ commits = $historicoSite } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $destinoSite 'status.json') -Encoding utf8
New-Item -ItemType File -Path (Join-Path $destinoSite '.nojekyll') -Force | Out-Null
Write-Output "Site pronto: $destinoSite"
