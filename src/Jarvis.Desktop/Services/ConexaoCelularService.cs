using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using Jarvis.Core;
using Jarvis.Server;
namespace Jarvis.Desktop.Services;
public sealed class ConexaoCelularService : IConexaoCelular
{
    public string ArquivoApk => Path.Combine(AppContext.BaseDirectory, "mobile", "Jarvis.apk");
    public string? CelularOnline => PresencaRemota.NomeOnline;
    public byte[] ObterQrInstalacao(string url) => PareamentoService.QrPng(url);
    public InstalacaoMobile ObterInstalacao()
    {
        var addresses = AparelhosIp.Candidatos().Select(a => a.Ip).Distinct().ToArray();
        var local = addresses.FirstOrDefault(ip => !EnderecosJarvis.Tailscale($"https://{ip}:5189")) ?? addresses.FirstOrDefault() ?? "127.0.0.1";
        var url = $"http://{local}:5188/mobile";
        return new(url, PareamentoService.QrPng(url), null);
    }
    public async Task<PareamentoQr> CriarPareamentoAsync(CancellationToken ct = default)
    {
        using var http = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, cert, _, _) => cert is not null && Convert.ToHexString(SHA256.HashData(cert.RawData)).Equals(CertificadoCache.Impressao, StringComparison.OrdinalIgnoreCase) }) { Timeout = TimeSpan.FromSeconds(5) };
        using var response = await http.GetAsync("https://127.0.0.1:5189/api/parear/iniciar", ct); response.EnsureSuccessStatusCode();
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var root = json.RootElement;
        return new(root.GetProperty("codigo").GetString()!, Convert.FromBase64String(root.GetProperty("qrPng").GetString()!), root.GetProperty("ip").GetString()!, root.GetProperty("porta").GetInt32(), DateTimeOffset.UtcNow.AddMinutes(5));
    }
}
