using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using Jarvis.Core;
using Jarvis.Server;
using Microsoft.Extensions.DependencyInjection;
namespace Jarvis.Tests;

public sealed class ToqueRemoto162Test
{
    [Fact] public void FaixasPretasNaoDeslocamOCliqueEMonitoresPodemTerOrigemNegativa()
    {
        Assert.Null(CoordenadasTela.Normalizar(200, 40, 400, 800, 1920, 1080));
        var center = CoordenadasTela.Normalizar(200, 400, 400, 800, 1920, 1080)!.Value;
        Assert.Equal(.5, center.X, 6); Assert.Equal(.5, center.Y, 6);
        var topLeft = CoordenadasTela.Normalizar(0, 287.5, 400, 800, 1920, 1080)!.Value;
        Assert.Equal(new PontoTela(0, 0), topLeft);
        var screen = new RegiaoJanela(-2560, -120, 2560, 1440);
        Assert.Equal((-2560, -120), CoordenadasTela.Pixel(topLeft, screen));
        Assert.Equal((-1, 1319), CoordenadasTela.Pixel(new(1, 1), screen));
        Assert.Null(CoordenadasTela.Normalizar(double.NaN, 0, 400, 800, 1920, 1080));
        Assert.Null(CoordenadasTela.Normalizar(0, 0, 0, 800, 1920, 1080));
    }
    [Fact] public void GestosRecusamAcaoLivreCoordenadasInvalidasEEntradaExcessiva()
    {
        var ok = new PedidoToque("monitor", "clique"); ok.Validar();
        foreach (var bad in new[] { ok with { Acao = "powershell" }, ok with { X = double.NaN }, ok with { DestinoY = 1.01 }, ok with { DuracaoMs = 30000 }, ok with { Acao = "rolar", Rolagem = 13 }, ok with { Acao = "tecla", Tecla = "Win+R" }, ok with { MonitorId = "" } })
            Assert.Throws<ArgumentException>(bad.Validar);
    }
    [Fact] public async Task EntradaNativaComecaBloqueadaEExigeTransmissaoRecente()
    {
        var tela = new TelaFalsa(); var input = new Jarvis.Sistema.EntradaRemota(tela);
        Assert.False(input.Permitida);
        await Assert.ThrowsAsync<InvalidOperationException>(() => input.ExecutarAsync(new("monitor", "clique")));
        Assert.False(input.Permitida); tela.Permitida = true; Assert.True(input.Permitida);
        await Assert.ThrowsAsync<InvalidOperationException>(() => input.ExecutarAsync(new("monitor", "clique")));
        tela.Permitida = false; Assert.False(input.Permitida);
    }
    private sealed class TelaFalsa : ITransmissaoTela
    {
        public bool Permitida { get; set; }
        public DateTimeOffset? UltimaVisualizacao => null;
        public QuadroTela Capturar() => throw new NotSupportedException();
    }
    private static int Port() { using var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); return ((IPEndPoint)listener.LocalEndpoint).Port; }
    [Fact] public async Task EndpointExigeHttpsTokenEPermissaoEEntregaOMonitorSelecionado()
    {
        var fake = new PcFalso(); using var cert = CertificadoLocal.Gerar("127.0.0.1"); var plain = Port(); var secure = Port();
        await using var app = HospedeJarvis.Criar([], plain, secure, cert, fake); await app.StartAsync();
        var devices = app.Services.GetRequiredService<Aparelhos>(); var token = devices.Parear("Teste toque temporário");
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{plain}") };
            using var https = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, c, _, _) => c?.Thumbprint == cert.Thumbprint }) { BaseAddress = new Uri($"https://127.0.0.1:{secure}") };
            var gesture = new PedidoToque("SEGUNDO-MONITOR", "arrastar", .1, .2, .8, .7);
            Assert.Equal(HttpStatusCode.Forbidden, (await http.PostAsJsonAsync("/api/tela/entrada", gesture)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await https.PostAsJsonAsync("/api/tela/entrada", gesture)).StatusCode);
            https.DefaultRequestHeaders.Add("X-Jarvis-Token", token);
            Assert.False((await https.GetFromJsonAsync<EstadoEntrada>("/api/tela/entrada"))!.Permitida);
            Assert.Equal((HttpStatusCode)423, (await https.PostAsJsonAsync("/api/tela/entrada", gesture)).StatusCode);
            fake.Permitida = true;
            (await https.PostAsJsonAsync("/api/tela/entrada", gesture)).EnsureSuccessStatusCode(); Assert.Equal(gesture, fake.Ultimo);
            Assert.Equal(HttpStatusCode.BadRequest, (await https.PostAsJsonAsync("/api/tela/entrada", gesture with { Acao = "cmd" })).StatusCode);
            fake.Permitida = false;
            Assert.Equal((HttpStatusCode)423, (await https.PostAsJsonAsync("/api/tela/entrada", gesture)).StatusCode);
            devices.Remover(token);
            Assert.Equal(HttpStatusCode.Unauthorized, (await https.PostAsJsonAsync("/api/tela/entrada", gesture)).StatusCode);
        }
        finally { devices.Remover(token); await app.StopAsync(); }
    }
    private sealed class PcFalso : IControleRemoto
    {
        public bool Permitida; public PedidoToque? Ultimo;
        public EstadoEntrada Entrada() => new(Permitida, "Bloqueado no PC");
        public Task ToqueAsync(PedidoToque p, CancellationToken ct) { if (!Permitida) throw new InvalidOperationException("Bloqueado no PC"); Ultimo = p; return Task.CompletedTask; }
        public IReadOnlyList<AppInfo> Apps() => [];
        public Task<string> ExecutarAsync(string comando, string? id, bool confirmado) => Task.FromResult("");
    }
    [Fact] public void ReatorMobileRenderizaPosesDistintasSemDependenciaDoAndroid()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory); while (root is not null && !File.Exists(Path.Combine(root.FullName, "Jarvis.sln"))) root = root.Parent;
        var folder = Path.Combine(root!.FullName, "artifacts", "preview"); Directory.CreateDirectory(folder);
        byte[] Render(double time)
        {
            using var surface = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(720, 720));
            Jarvis.Mobile.Views.ReatorDetalhado.Desenhar(surface.Canvas, 720, 720, time, .1);
            using var snapshot = surface.Snapshot(); using var backdrop = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(720, 720)); backdrop.Canvas.Clear(SkiaSharp.SKColor.Parse("#050D18")); backdrop.Canvas.DrawImage(snapshot, 0, 0);
            using var opaque = backdrop.Snapshot(); using var png = opaque.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100); return png.ToArray();
        }
        var a = Render(0); var b = Render(4); Assert.False(a.SequenceEqual(b));
        File.WriteAllBytes(Path.Combine(folder, "ReatorMobile162.png"), a); File.WriteAllBytes(Path.Combine(folder, "ReatorMobile162-giro.png"), b);
    }
}
