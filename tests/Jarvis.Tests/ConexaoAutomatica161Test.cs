using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using Jarvis.Core;
using Jarvis.Server;
using Microsoft.Extensions.DependencyInjection;
namespace Jarvis.Tests;

public sealed class ConexaoAutomatica161Test
{
    [Theory]
    [InlineData("https://100.64.0.1:5189", true)] [InlineData("https://100.127.255.1:5189", true)] [InlineData("https://100.63.0.1:5189", false)] [InlineData("https://100.128.0.1:5189", false)] [InlineData("https://pc.tailnet.ts.net:5189", true)] [InlineData("https://pc.ts.net.exemplo.com:5189", false)]
    public void ReconheceApenasFaixaDaVpn(string url, bool expected) => Assert.Equal(expected, EnderecosJarvis.Tailscale(url));
    [Fact] public void EnderecosAprendidosExigemHttpsSemCredenciaisNemConsulta()
    {
        var urls = EnderecosJarvis.Reunir(["http://192.168.0.2:5189", "https://a:b@100.64.0.2:5189", "https://100.64.0.2:5189/?token=abc", "https://100.64.0.2:5189/", "https://100.64.0.2:5189"]);
        Assert.Equal("https://100.64.0.2:5189", Assert.Single(urls));
    }
    private static int Port() { using var t = new TcpListener(IPAddress.Loopback, 0); t.Start(); return ((IPEndPoint)t.LocalEndpoint).Port; }
    [Fact] public async Task ReconectaEmOutroEnderecoSemIgnorarCertificadoOuToken()
    {
        using var cert = CertificadoLocal.Gerar("127.0.0.1"); var pin = Convert.ToHexString(SHA256.HashData(cert.RawData)); var plain = Port(); var secure = Port();
        await using var app = HospedeJarvis.Criar([], plain, secure, cert); await app.StartAsync(); var devices = app.Services.GetRequiredService<Aparelhos>(); var token = devices.Parear("Teste reconexão temporário"); var url = $"https://127.0.0.1:{secure}";
        try
        {
            var results = await ConexaoPareada.ProcurarAsync([$"https://127.0.0.1:{Port()}", url], pin, token); var live = Assert.Single(results.Where(r => r.Conectado)); Assert.Equal(url, live.Endereco); Assert.NotNull(live.Identidade); Assert.NotNull(live.Identidade.Enderecos);
            var wrongPin = await ConexaoPareada.VerificarAsync(url, new string('0', 64), token); Assert.False(wrongPin.Conectado); Assert.Contains("certificado", wrongPin.Mensagem);
            var wrongToken = await ConexaoPareada.VerificarAsync(url, pin, "revogado"); Assert.False(wrongToken.Conectado); Assert.Contains("Token recusado", wrongToken.Mensagem);
            using var life = new CancellationTokenSource(); life.Cancel(); await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ConexaoPareada.ProcurarAsync([url, url + "/", $"https://127.0.0.1:{Port()}"], pin, token, life.Token));
        }
        finally { devices.Remover(token); await app.StopAsync(); }
    }
    [Fact] public async Task DownloadSemUsbEPareamentoIncluemEnderecosDoPc()
    {
        var file = Path.Combine(Path.GetTempPath(), "jarvis-download-" + Guid.NewGuid().ToString("N") + ".apk"); await File.WriteAllBytesAsync(file, Enumerable.Range(0, 8192).Select(i => (byte)i).ToArray());
        using var cert = CertificadoLocal.Gerar("127.0.0.1"); var plain = Port(); var secure = Port(); await using var app = HospedeJarvis.Criar([], plain, secure, cert, apk: file); await app.StartAsync(); string? token = null;
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{plain}") };
            using var page = await http.GetAsync("/mobile"); page.EnsureSuccessStatusCode(); Assert.Contains("SEM CABO USB", await page.Content.ReadAsStringAsync());
            using var request = new HttpRequestMessage(HttpMethod.Get, "/jarvis.apk"); request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 99);
            using var download = await http.SendAsync(request); Assert.Equal(HttpStatusCode.PartialContent, download.StatusCode); Assert.Equal(100, (await download.Content.ReadAsByteArrayAsync()).Length);
            Assert.True(download.Headers.CacheControl!.Private); Assert.True(download.Headers.CacheControl.MustRevalidate); Assert.NotNull(download.Headers.ETag);
            Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync("/api/identidade")).StatusCode);
            using var https = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, c, _, _) => c?.Thumbprint == cert.Thumbprint }) { BaseAddress = new Uri($"https://127.0.0.1:{secure}") };
            var oneTime = app.Services.GetRequiredService<PareamentoService>().EmitirToken(); using var paired = await https.PostAsJsonAsync("/api/parear/confirmar", new { token = oneTime, nome = "Teste pareamento temporário" }); paired.EnsureSuccessStatusCode(); using var body = await JsonDocument.ParseAsync(await paired.Content.ReadAsStreamAsync()); token = body.RootElement.GetProperty("token").GetString(); var addresses = body.RootElement.GetProperty("enderecos").GetString()!.Split('|', StringSplitOptions.RemoveEmptyEntries); Assert.All(addresses, e => Assert.NotNull(EnderecosJarvis.Normalizar(e))); Assert.True(body.RootElement.TryGetProperty("remoto", out _));
        }
        finally { if (token is not null) app.Services.GetRequiredService<Aparelhos>().Remover(token); await app.StopAsync(); File.Delete(file); }
    }
}

