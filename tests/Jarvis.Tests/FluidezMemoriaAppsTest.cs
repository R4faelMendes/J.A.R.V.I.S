using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text.Json;
using Jarvis.Core;
using Jarvis.Server;
using Jarvis.Sistema;
using Microsoft.Extensions.DependencyInjection;

namespace Jarvis.Tests;
public sealed class FluidezMemoriaAppsTest
{
    [Fact]
    public void RamRealUsaEstruturaCompleta()
    {
        Assert.Equal(64, MemoriaWindows.TamanhoEstrutura);
        var ram = MemoriaWindows.Ler(); Assert.NotNull(ram);
        Assert.True(ram.TotalBytes > 0); Assert.InRange(ram.Percentual, 0, 100);
        Assert.InRange(ram.UsadaBytes, 0UL, ram.TotalBytes);
    }
    [Fact]
    public void CatalogoSoAceitaIdentificadoresValidados()
    {
        var apps = new AppController();
        var disponiveis = apps.ListarDisponiveis();
        Assert.Equal(disponiveis.Count, disponiveis.Select(a => a.Id).Distinct().Count());
        Assert.All(apps.ListarApps(), a => Assert.Contains(disponiveis, b => a.Id == b.Id));
        Assert.Throws<ArgumentException>(() => apps.Abrir("cmd.exe /c echo proibido"));
        Assert.Throws<ArgumentException>(() => apps.SelecionarApps(new[] { "inexistente" }));
    }
    [Fact]
    public void TransmissaoValidaLimitesENaoCapturaSemPermissao()
    {
        new OpcoesTransmissao().Validar();
        Assert.Throws<ArgumentException>(() => new OpcoesTransmissao(1000).Validar());
        Assert.Throws<ArgumentException>(() => new OpcoesTransmissao(Largura: 99999).Validar());
        var tela = new TransmissaoTela();
        Assert.Throws<InvalidOperationException>(() => tela.AbrirSessao(null, new()));
    }
    [Fact]
    public void CapturaContinuaReutilizaSessaoEParaAoRevogar()
    {
        var tela = new TransmissaoTela { Permitida = true };
        var measures = new List<object>();
        try
        {
            foreach (var width in new[] { 960, 1280 })
            {
                using var session = tela.AbrirSessao(null, new(60, width));
                session.Capturar(); var watch = Stopwatch.StartNew(); long bytes = 0;
                for (var i = 0; i < 60; i++) { var frame = session.Capturar(); Assert.Equal((byte)216, frame.Jpeg[1]); Assert.InRange(frame.Largura, 1, width); bytes += frame.Jpeg.Length; }
                watch.Stop(); measures.Add(new { largura = width, motor = session.Motor, capturasPorSegundo = 60 / watch.Elapsed.TotalSeconds, mediaMs = watch.Elapsed.TotalMilliseconds / 60, mediaBytes = bytes / 60 });
                tela.Permitida = false; Assert.Throws<InvalidOperationException>(() => session.Capturar()); tela.Permitida = true;
            }
            var root = new DirectoryInfo(AppContext.BaseDirectory); while (root is not null && !File.Exists(Path.Combine(root.FullName, "Jarvis.sln"))) root = root.Parent;
            var target = Path.Combine(root!.FullName, "artifacts", "verificacao-fluidez.json"); Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllText(target, JsonSerializer.Serialize(measures, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally { tela.Permitida = false; }
    }
    private sealed class Pc : IControleRemoto
    {
        public int Quadros;
        public IReadOnlyList<AppInfo> Apps() => [];
        public Task<string> ExecutarAsync(string comando, string? id, bool confirmado) => Task.FromResult("ok");
        public ISessaoTela AbrirTransmissao(string? monitor, OpcoesTransmissao opcoes) { Assert.Equal("segunda", monitor); return new Session(this); }
        private sealed class Session(Pc pc) : ISessaoTela { public QuadroTela Capturar() { Interlocked.Increment(ref pc.Quadros); return new([255,216,255,217], 1, 1, DateTimeOffset.UtcNow); } public void Dispose() { } }
    }
    private static int Porta() { using var tcp = new TcpListener(IPAddress.Loopback, 0); tcp.Start(); return ((IPEndPoint)tcp.LocalEndpoint).Port; }
    [Fact]
    public async Task StreamExigeTokenEAplicaContrapressao()
    {
        using var cert = CertificadoLocal.Gerar("127.0.0.1"); var pc = new Pc(); var httpPort = Porta(); var httpsPort = Porta();
        await using var app = HospedeJarvis.Criar([], httpPort, httpsPort, cert, pc); await app.StartAsync();
        var aparelhos = app.Services.GetRequiredService<Aparelhos>(); string? token = null;
        try
        {
            using var https = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, c, _, _) => c?.Thumbprint == cert.Thumbprint }) { BaseAddress = new($"https://127.0.0.1:{httpsPort}") };
            Assert.Equal(HttpStatusCode.Unauthorized, (await https.GetAsync("/api/tela/stream")).StatusCode);
            using var http = new HttpClient(); Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync($"http://127.0.0.1:{httpPort}/api/tela/stream")).StatusCode);
            token = aparelhos.Parear("Teste stream temporário");
            using var socket = new ClientWebSocket(); socket.Options.SetRequestHeader("X-Jarvis-Token", token); socket.Options.RemoteCertificateValidationCallback = (_, c, _, _) => c?.GetCertHashString() == cert.Thumbprint;
            using var life = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await socket.ConnectAsync(new($"wss://127.0.0.1:{httpsPort}/api/tela/stream?monitor=segunda&fps=60&largura=960"), life.Token);
            var buffer = new byte[16];
            for (var i = 0; i < 2; i++) { var frame = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), life.Token); Assert.Equal(4, frame.Count); Assert.True(frame.EndOfMessage); }
            await Task.Delay(100, life.Token); Assert.Equal(2, pc.Quadros);
            await socket.SendAsync(new ArraySegment<byte>(new byte[] { 1 }), WebSocketMessageType.Binary, true, life.Token);
            await socket.ReceiveAsync(new ArraySegment<byte>(buffer), life.Token); Assert.Equal(3, pc.Quadros);
            socket.Abort();
        }
        finally { if (token is not null) aparelhos.Remover(token); await app.StopAsync(); }
    }
}
