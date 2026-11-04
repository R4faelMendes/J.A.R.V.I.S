using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using Jarvis.Core;
using Jarvis.Server;
using Microsoft.Extensions.DependencyInjection;
namespace Jarvis.Tests;

public sealed class Aparelho170Test
{
    private static DadosAparelho Dados => new("Aparelho de teste", "Android 14", 72, true, 30_000_000_000, 128_000_000_000, "Wi-Fi", true);
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = new(2026, 10, 19, 18, 0, 0, TimeSpan.Zero); public override DateTimeOffset GetUtcNow() => Now; }
    private static byte[] Jpeg()
    {
        using var surface = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(240, 480)); surface.Canvas.Clear(SkiaSharp.SKColor.Parse("#123049"));
        using var snapshot = surface.Snapshot(); using var data = snapshot.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 65); return data.ToArray();
    }
    [Fact] public void PainelUsaDadosReaisDoProtocoloEPresencaExpira()
    {
        var clock = new Clock(); var central = new CentralAparelhos(clock);
        Assert.Empty(central.Listar()); central.Pulso("segredo-A", "Celular A", Dados);
        var a = Assert.Single(central.Listar()); Assert.Equal(Dados, a.Dados); Assert.True(a.Online); Assert.DoesNotContain("segredo", a.Id);
        clock.Now = clock.Now.AddSeconds(26); Assert.False(Assert.Single(central.Listar()).Online);
        Assert.Throws<InvalidOperationException>(() => central.Encontrar(a.Id));
        Assert.Throws<ArgumentException>(() => central.Pulso("segredo-A", "A", Dados with { Bateria = 101 }));
        Assert.Throws<ArgumentException>(() => central.Pulso("segredo-A", "A", Dados with { ArmazenamentoLivre = 130_000_000_000 }));
    }
    [Fact] public void LocalizarTemConfirmacaoIsolamentoExpiracaoESilenciar()
    {
        var clock = new Clock(); var central = new CentralAparelhos(clock);
        central.Pulso("A", "A", Dados); central.Pulso("B", "B", Dados);
        var id = CentralAparelhos.IdToken("A"); central.Encontrar(id);
        var order = Assert.Single(central.Pulso("A", "A", Dados)); Assert.Equal("localizar", order.Acao);
        Assert.Empty(central.Pulso("B", "B", Dados));
        central.Confirmar("B", new(order.Id, "tocando")); Assert.Equal("Solicitação enviada", central.Listar().Single(a => a.Id == id).Localizacao);
        central.Confirmar("A", new(order.Id, "tocando")); Assert.Contains("confirmado", central.Listar().Single(a => a.Id == id).Localizacao); Assert.Empty(central.Pulso("A", "A", Dados));
        central.Silenciar(id); var stop = Assert.Single(central.Pulso("A", "A", Dados)); Assert.Equal("silenciar", stop.Acao); central.Confirmar("A", new(stop.Id, "silenciado"));
        Assert.Contains("interrompido", central.Listar().Single(a => a.Id == id).Localizacao);
        central.Encontrar(id); clock.Now = clock.Now.AddSeconds(21); Assert.Contains("Sem confirmação", central.Listar().Single(a => a.Id == id).Localizacao); Assert.Empty(central.Pulso("A", "A", Dados));
    }
    [Fact] public void EspelhoNaoMisturaAparelhosOuSessoesELimpaAoParar()
    {
        var clock = new Clock(); var central = new CentralAparelhos(clock); central.Pulso("A", "A", Dados); central.Pulso("B", "B", Dados);
        var id = CentralAparelhos.IdToken("A"); var session = central.IniciarEspelho("A"); var jpeg = Jpeg(); central.Receber("A", session, jpeg);
        Assert.Equal(240, central.Quadro(id)!.Largura); Assert.Null(central.Quadro(CentralAparelhos.IdToken("B")));
        Assert.Throws<InvalidOperationException>(() => central.Receber("B", session, jpeg));
        var newSession = central.IniciarEspelho("A"); central.Encerrar("A", session); central.Receber("A", newSession, jpeg);
        Assert.Throws<InvalidOperationException>(() => central.Receber("A", session, jpeg));
        clock.Now = clock.Now.AddSeconds(4); Assert.Null(central.Quadro(id));
        central.Receber("A", newSession, jpeg); central.PararEspelho(id); Assert.Null(central.Quadro(id));
        Assert.Throws<InvalidOperationException>(() => central.Receber("A", newSession, jpeg)); Assert.Equal("parar-espelho", Assert.Single(central.Pulso("A", "A", Dados)).Acao);
        central.IniciarEspelho("A"); Assert.Empty(central.Pulso("A", "A", Dados));
    }
    [Fact] public void JpegRejeitaPayloadTruncadoExcessoDeBytesEDimensoesExcessivas()
    {
        var jpeg = Jpeg(); Assert.Equal((240, 480), JpegAparelho.Validar(jpeg));
        Assert.Throws<ArgumentException>(() => JpegAparelho.Validar(jpeg[..^2]));
        Assert.Throws<ArgumentException>(() => JpegAparelho.Validar(new byte[JpegAparelho.LimiteBytes + 1]));
        for (var i = 0; i < jpeg.Length - 10; i++) if (jpeg[i] == 255 && jpeg[i + 1] == 192) { jpeg[i + 7] = 127; jpeg[i + 8] = 255; break; }
        Assert.Throws<ArgumentException>(() => JpegAparelho.Validar(jpeg));
    }
    private static int Port() { using var l = new TcpListener(IPAddress.Loopback, 0); l.Start(); return ((IPEndPoint)l.LocalEndpoint).Port; }
    [Fact] public async Task RotasExigemHttpsTokenESessaoERecusamUploadDepoisDaParada()
    {
        using var cert = CertificadoLocal.Gerar("127.0.0.1"); var plain = Port(); var secure = Port(); var central = new CentralAparelhos();
        await using var app = HospedeJarvis.Criar([], plain, secure, cert, central: central); await app.StartAsync();
        var devices = app.Services.GetRequiredService<Aparelhos>(); var token = devices.Parear("Teste aparelho temporário");
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{plain}") };
            using var https = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, c, _, _) => c?.Thumbprint == cert.Thumbprint }) { BaseAddress = new Uri($"https://127.0.0.1:{secure}") };
            Assert.Equal(HttpStatusCode.Forbidden, (await http.PostAsJsonAsync("/api/celular/pulso", Dados)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await https.PostAsJsonAsync("/api/celular/pulso", Dados)).StatusCode);
            https.DefaultRequestHeaders.Add("X-Jarvis-Token", token);
            (await https.PostAsJsonAsync("/api/celular/pulso", Dados)).EnsureSuccessStatusCode();
            using var started = await https.PostAsync("/api/celular/tela/iniciar", null); started.EnsureSuccessStatusCode(); var session = (await started.Content.ReadFromJsonAsync<SessaoEspelho>())!.Id;
            using var payload = new ByteArrayContent(Jpeg()); (await https.PostAsync("/api/celular/tela/quadro?sessao=" + session, payload)).EnsureSuccessStatusCode();
            Assert.NotNull(central.Quadro(CentralAparelhos.IdToken(token)));
            (await https.PostAsJsonAsync("/api/celular/tela/parar", new SessaoEspelho(session))).EnsureSuccessStatusCode(); Assert.Null(central.Quadro(CentralAparelhos.IdToken(token)));
            using var late = new ByteArrayContent(Jpeg()); Assert.Equal(HttpStatusCode.Conflict, (await https.PostAsync("/api/celular/tela/quadro?sessao=" + session, late)).StatusCode);
            using var oversized = new ByteArrayContent(new byte[JpegAparelho.LimiteBytes + 1]); Assert.Equal((HttpStatusCode)413, (await https.PostAsync("/api/celular/tela/quadro?sessao=" + session, oversized)).StatusCode);
            devices.Remover(token); Assert.Equal(HttpStatusCode.Unauthorized, (await https.PostAsJsonAsync("/api/celular/pulso", Dados)).StatusCode);
        }
        finally { devices.Remover(token); await app.StopAsync(); }
    }
}
