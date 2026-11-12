using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jarvis.Core;
namespace Jarvis.Desktop.Services;

public sealed class AtualizadorJarvis : IAtualizadorJarvis
{
    private readonly HttpClient _http;
    private readonly string _pasta;
    private string? _verificado, _hash;
    public string VersaoAtual { get; }
    private const string Base = "https://github.com/R4faelMendes/J.A.R.V.I.S/releases/";
    public AtualizadorJarvis(HttpClient? http = null, string? pasta = null, string? versao = null)
    {
        _http = http ?? new HttpClient { Timeout = TimeSpan.FromMinutes(20) };
        if (!_http.DefaultRequestHeaders.Contains("User-Agent")) _http.DefaultRequestHeaders.UserAgent.ParseAdd("Jarvis-Atualizador/2.4");
        _pasta = pasta ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis", "atualizacoes");
        VersaoAtual = versao ?? (Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "2.4.0");
    }
    public static void ValidarUrl(Uri uri, string arquivo)
    {
        if (uri.Scheme != "https" || !uri.IsDefaultPort || uri.Host != "github.com" || uri.Query.Length != 0 || uri.Fragment.Length != 0 ||
            !Regex.IsMatch(uri.AbsolutePath, @"^/R4faelMendes/J\.A\.R\.V\.I\.S/releases/download/v\d+\.\d+\.\d+/" + Regex.Escape(arquivo) + "$", RegexOptions.CultureInvariant))
            throw new InvalidDataException("O endereço não pertence à distribuição oficial do Jarvis.");
    }
    public async Task<VersaoDisponivel?> ConsultarAsync(CancellationToken ct)
    {
        using var resposta = await _http.GetAsync("https://api.github.com/repos/R4faelMendes/J.A.R.V.I.S/releases/latest", ct);
        resposta.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(ct)); var root = json.RootElement;
        var tag = root.GetProperty("tag_name").GetString() ?? "";
        if (!Regex.IsMatch(tag, @"^v\d+\.\d+\.\d+$") || !Version.TryParse(tag[1..], out var disponivel)) throw new InvalidDataException("Versão inválida na distribuição.");
        if (disponivel <= Version.Parse(VersaoAtual)) return null;
        var assets = root.GetProperty("assets").EnumerateArray().ToArray();
        Uri Asset(string nome) { var a = assets.Single(a => a.GetProperty("name").GetString() == nome); var uri = new Uri(a.GetProperty("browser_download_url").GetString()!); ValidarUrl(uri, nome); if (!uri.AbsolutePath.Contains("/" + tag + "/", StringComparison.Ordinal)) throw new InvalidDataException("Arquivos de versões diferentes."); return uri; }
        return new(tag[1..], root.GetProperty("body").GetString() ?? "Sem notas.", new Uri(Base + "tag/" + tag), Asset("Jarvis-Windows-x64.exe"), Asset("SHA256SUMS.txt"));
    }
    public static string LerChecksum(string texto)
    {
        var linhas = texto.Split('\n').Select(l => l.Trim()).Where(l => l.EndsWith("Jarvis-Windows-x64.exe", StringComparison.Ordinal)).ToArray();
        if (linhas.Length != 1) throw new InvalidDataException("Checksum ausente ou duplicado.");
        var m = Regex.Match(linhas[0], @"^([a-fA-F0-9]{64})\s+\*?Jarvis-Windows-x64\.exe$");
        if (!m.Success) throw new InvalidDataException("Checksum inválido.");
        return m.Groups[1].Value.ToUpperInvariant();
    }
    public async Task<string> BaixarAsync(VersaoDisponivel versao, IProgress<ProgressoDownload> progresso, CancellationToken ct)
    {
        ValidarUrl(versao.Executavel, "Jarvis-Windows-x64.exe"); ValidarUrl(versao.Checksums, "SHA256SUMS.txt");
        if (!Regex.IsMatch(versao.Versao, @"^\d+\.\d+\.\d+$") || !versao.Executavel.AbsolutePath.Contains("/v" + versao.Versao + "/", StringComparison.Ordinal) || !versao.Checksums.AbsolutePath.Contains("/v" + versao.Versao + "/", StringComparison.Ordinal)) throw new InvalidDataException("Arquivos de versões diferentes.");
        var hash = LerChecksum(await _http.GetStringAsync(versao.Checksums, ct));
        Directory.CreateDirectory(_pasta); var arquivo = Path.Combine(_pasta, "Jarvis-" + versao.Versao + ".exe"); var parcial = arquivo + ".part";
        try
        {
            using var response = await _http.GetAsync(versao.Executavel, HttpCompletionOption.ResponseHeadersRead, ct); response.EnsureSuccessStatusCode();
            await using (var origem = await response.Content.ReadAsStreamAsync(ct))
            await using (var destino = File.Create(parcial))
            {
                var buffer = new byte[131072]; long total = 0; int n;
                while ((n = await origem.ReadAsync(buffer, ct)) > 0) { await destino.WriteAsync(buffer.AsMemory(0, n), ct); total += n; progresso.Report(new("Baixando atualização", total, response.Content.Headers.ContentLength)); }
                if (response.Content.Headers.ContentLength is long esperado && total != esperado) throw new InvalidDataException("Download incompleto.");
            }
            if (await HashAsync(parcial, ct) != hash) throw new InvalidDataException("A verificação SHA-256 falhou. O arquivo não será instalado.");
            File.Move(parcial, arquivo, true); _verificado = arquivo; _hash = hash;
            progresso.Report(new("Download verificado por SHA-256", new FileInfo(arquivo).Length)); return arquivo;
        }
        finally { if (File.Exists(parcial)) File.Delete(parcial); }
    }
    private static async Task<string> HashAsync(string path, CancellationToken ct) { await using var f = File.OpenRead(path); return Convert.ToHexString(await SHA256.HashDataAsync(f, ct)); }
    public async Task InstalarAoSairAsync(string arquivo, CancellationToken ct)
    {
        if (arquivo != _verificado || _hash is null || await HashAsync(arquivo, ct) != _hash) throw new InvalidDataException("Baixe e verifique novamente a atualização antes de instalar.");
        var alvo = Environment.ProcessPath ?? throw new InvalidOperationException("Executável atual indisponível.");
        if (!Path.GetFileName(alvo).Equals("Jarvis.exe", StringComparison.OrdinalIgnoreCase) && !Path.GetFileName(alvo).Equals("Jarvis-Windows-x64.exe", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Atualização automática disponível no executável portátil publicado.");
        var script = Path.Combine(_pasta, "instalar-" + Guid.NewGuid().ToString("N") + ".ps1");
        // Argumentos são passados por ArgumentList, sem interpolar caminhos no script.
        await File.WriteAllTextAsync(script, """
param([int]$Processo, [string]$Origem, [string]$Destino, [string]$Hash)
$ErrorActionPreference = 'Stop'
$backup = $Destino + '.anterior'
$movido = $false
try {
    $p = Get-Process -Id $Processo -ErrorAction SilentlyContinue
    if ($p) { if (-not $p.WaitForExit(60000)) { throw 'O Jarvis continua aberto. Atualização cancelada.' } }
    if ((Get-FileHash -LiteralPath $Origem -Algorithm SHA256).Hash -ne $Hash) { throw 'SHA-256 divergente.' }
    if (Test-Path -LiteralPath $backup) { Remove-Item -LiteralPath $backup }
    Move-Item -LiteralPath $Destino -Destination $backup
    $movido = $true
    Copy-Item -LiteralPath $Origem -Destination $Destino
    Start-Process -FilePath $Destino -WorkingDirectory (Split-Path -Parent $Destino) -WindowStyle Hidden
} catch {
    if ($movido) {
        if (Test-Path -LiteralPath $Destino) { Remove-Item -LiteralPath $Destino }
        Move-Item -LiteralPath $backup -Destination $Destino
        Start-Process -FilePath $Destino -WorkingDirectory (Split-Path -Parent $Destino) -WindowStyle Hidden
    }
    $_.Exception.Message | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'ultima-atualizacao.log')
}
""", ct);
        var start = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = _pasta };
        foreach (var arg in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", script, "-Processo", Environment.ProcessId.ToString(), "-Origem", arquivo, "-Destino", alvo, "-Hash", _hash }) start.ArgumentList.Add(arg);
        using var helper = Process.Start(start) ?? throw new InvalidOperationException("Não foi possível iniciar o atualizador.");
    }
}
