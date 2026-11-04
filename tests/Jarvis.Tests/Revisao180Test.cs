using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Jarvis.Core;
using Jarvis.Data;
using Jarvis.Server;

namespace Jarvis.Tests;

public sealed class Revisao180Test
{
    [Theory]
    [InlineData(75, 100, 75)] [InlineData(150, 200, 75)] [InlineData(0, 100, 0)] [InlineData(100, 100, 100)]
    [InlineData(-1, 100, null)] [InlineData(1, 0, null)] [InlineData(101, 100, null)]
    public void BateriaPreservaZeroERejeitaEscalaInvalida(int level, int scale, int? expected)
        => Assert.Equal(expected, BateriaAparelho.Percentual(level, scale));

    [Fact] public void AgendaBloqueiaDiasPassadosSemImpedirExclusaoDeHistorico()
    {
        var today = DateTime.Today;
        Assert.False(AgendaDatas.PodeSelecionar(today.AddDays(-1), today)); Assert.True(AgendaDatas.PodeSelecionar(today, today)); Assert.True(AgendaDatas.PodeSelecionar(today.AddDays(1), today));
        var past = new Compromisso(Guid.NewGuid().ToString(), "Passado", new DateTimeOffset(today.AddDays(-1).AddHours(10)), new DateTimeOffset(today.AddDays(-1).AddHours(11)));
        Assert.Throws<ArgumentException>(() => AgendaDatas.Validar(past, today)); AgendaDatas.Validar(past with { Excluido = true }, today);
        var path = Path.Combine(Path.GetTempPath(), "jarvis-180-" + Guid.NewGuid().ToString("N") + ".db");
        try { var service = new AgendaService(path); Assert.Throws<ArgumentException>(() => service.Salvar(past)); Assert.Empty(service.Listar()); }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.Delete(path); }
    }

    [Fact] public void DescobertaPrefereOrigemLocalERecusaRespostasInvalidas()
    {
        var bytes = Encoding.UTF8.GetBytes("""{"app":"Jarvis","nome":"MEU-PC","https":5189,"ips":["100.64.1.1","172.20.1.1"]}""");
        var pc = DescobertaRede.Interpretar(bytes, IPAddress.Parse("192.168.0.64")); Assert.Equal(new PcNaRede("192.168.0.64", "MEU-PC"), pc);
        Assert.Null(DescobertaRede.Interpretar(bytes, IPAddress.Parse("8.8.8.8"))); Assert.Null(DescobertaRede.Interpretar(bytes, IPAddress.Parse("100.64.1.1")));
        Assert.Null(DescobertaRede.Interpretar("broken"u8.ToArray(), IPAddress.Parse("192.168.0.1")));
        Assert.Null(DescobertaRede.Interpretar(Encoding.UTF8.GetBytes("""{"app":"Jarvis","nome":"PC","https":4444}"""), IPAddress.Parse("192.168.0.1")));
        Assert.Equal(IPAddress.Parse("192.168.3.255"), DescobertaRede.Broadcast(IPAddress.Parse("192.168.2.50"), IPAddress.Parse("255.255.254.0")));
    }
    private static int Port() { using var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); return ((IPEndPoint)listener.LocalEndpoint).Port; }

    [Fact] public async Task InstaladorOfereceSomenteUniversalEPermiteRetomadaSemExporApi()
    {
        var directory = Path.Combine(Path.GetTempPath(), "jarvis-installer-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        var apk = Path.Combine(directory, "Jarvis.apk"); var arm64 = Path.Combine(directory, "Jarvis-arm64.apk");
        var universalBytes = Enumerable.Range(0, 2048).Select(i => (byte)i).ToArray(); var armBytes = universalBytes[..512];
        await File.WriteAllBytesAsync(apk, universalBytes); await File.WriteAllBytesAsync(arm64, armBytes);
        var plain = Port(); var tls = Port(); using var cert = CertificadoLocal.Gerar("127.0.0.1");
        await using var server = HospedeJarvis.Criar([], plain, tls, cert, apk: apk); await server.StartAsync();
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{plain}") };
            using var manifest = await http.GetAsync("/mobile/manifesto"); manifest.EnsureSuccessStatusCode();
            using var json = System.Text.Json.JsonDocument.Parse(await manifest.Content.ReadAsStringAsync()); var packages = json.RootElement.GetProperty("pacotes");
            Assert.Equal(1, packages.GetArrayLength()); Assert.Equal(2048, packages[0].GetProperty("bytes").GetInt64());
            Assert.Equal(Convert.ToHexString(SHA256.HashData(universalBytes)), packages[0].GetProperty("sha256").GetString());
            using var first = await http.GetAsync("/jarvis.apk"); first.EnsureSuccessStatusCode(); Assert.Equal(universalBytes, await first.Content.ReadAsByteArrayAsync()); Assert.NotNull(first.Headers.ETag);
            using var request = new HttpRequestMessage(HttpMethod.Get, "/jarvis.apk"); request.Headers.Range = new(128, null); request.Headers.IfRange = new(first.Headers.ETag!);
            using var partial = await http.SendAsync(request); Assert.Equal(HttpStatusCode.PartialContent, partial.StatusCode); Assert.Equal(universalBytes[128..], await partial.Content.ReadAsByteArrayAsync());
            Assert.Equal(HttpStatusCode.BadRequest, (await http.GetAsync("/jarvis.apk?arquitetura=arm64")).StatusCode);
            using var head = await http.SendAsync(new HttpRequestMessage(HttpMethod.Head, "/jarvis.apk")); Assert.Equal(HttpStatusCode.OK, head.StatusCode); Assert.Equal(2048, head.Content.Headers.ContentLength);
            Assert.Equal(HttpStatusCode.BadRequest, (await http.GetAsync("/jarvis.apk?arquitetura=../else")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync("/api/celular/pulso")).StatusCode);
        }
        finally { await server.StopAsync(); File.Delete(apk); File.Delete(arm64); Directory.Delete(directory); }
    }
}
