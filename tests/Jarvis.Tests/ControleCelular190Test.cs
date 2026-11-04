using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Jarvis.Core;
using Jarvis.Server;
namespace Jarvis.Tests;
public sealed class ControleCelular190Test
{
    private sealed class Clock : TimeProvider { public DateTimeOffset Now = DateTimeOffset.UtcNow; public override DateTimeOffset GetUtcNow() => Now; }
    private static byte[] Jpeg()
    {
        using var surface = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(240, 480)); surface.Canvas.Clear(SkiaSharp.SKColors.CornflowerBlue);
        using var image = surface.Snapshot(); using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 60); return data.ToArray();
    }
    private static DadosAparelho Dados => new("Telefone de teste", "Android", 80, false, null, null, "Wi-Fi", false, true);
    [Fact] public void ValidacaoRecusaComandosLivresETextoExcessivo()
    {
        var input = new EntradaCelular(Guid.NewGuid(), "tocar", 240, 480); input.Validar();
        foreach (var bad in new[] { input with { Acao = "shell" }, input with { X = double.NaN }, input with { Rotacao = 4 }, input with { Acao = "texto", Texto = new string('a', 1001) }, input with { Acao = "tecla", Tecla = "Win+R" }, input with { Texto = "indevido" }, input with { DuracaoMs = 99999 } }) Assert.Throws<ArgumentException>(bad.Validar);
    }
    [Fact] public async Task ControleIsolaAparelhosConfirmaEExpiraSemReexecutar()
    {
        var clock = new Clock(); var central = new CentralAparelhos(clock); central.Pulso("A", "A", Dados); central.Pulso("B", "B", Dados);
        var a = central.IniciarEspelho("A"); var b = central.IniciarEspelho("B"); central.Receber("A", a, Jpeg()); central.Receber("B", b, Jpeg());
        var input = new EntradaCelular(a, "texto", 240, 480, Texto: "Olá"); central.Controlar(CentralAparelhos.IdToken("A"), input);
        Assert.Empty(await central.PuxarEntradasAsync("B", b, default));
        var order = Assert.Single(await central.PuxarEntradasAsync("A", a, default)); Assert.Equal(input, order.Entrada);
        central.ConfirmarEntrada("B", new(order.Id, "ok")); Assert.Single(await central.PuxarEntradasAsync("A", a, default));
        central.ConfirmarEntrada("A", new(order.Id, "ok")); Assert.Empty(await central.PuxarEntradasAsync("A", a, default));
        central.Controlar(CentralAparelhos.IdToken("A"), input); clock.Now = clock.Now.AddSeconds(6); Assert.Empty(await central.PuxarEntradasAsync("A", a, default));
        Assert.Throws<InvalidOperationException>(() => central.Controlar(CentralAparelhos.IdToken("A"), input));
    }
    [Fact] public async Task RotacaoParadaENovaSessaoInvalidamGestosAntigos()
    {
        var central = new CentralAparelhos(); var id = CentralAparelhos.IdToken("A"); central.Pulso("A", "A", Dados); var session = central.IniciarEspelho("A"); central.Receber("A", session, Jpeg());
        var input = new EntradaCelular(session, "tocar", 240, 480); central.Controlar(id, input);
        central.Receber("A", session, Jpeg(), 2); Assert.Empty(await central.PuxarEntradasAsync("A", session, default)); Assert.Throws<InvalidOperationException>(() => central.Controlar(id, input));
        central.Controlar(id, input with { Rotacao = 2 }); central.PararEspelho(id); await Assert.ThrowsAsync<InvalidOperationException>(() => central.PuxarEntradasAsync("A", session, default));
        var next = central.IniciarEspelho("A"); central.Receber("A", next, Jpeg()); Assert.Throws<InvalidOperationException>(() => central.Controlar(id, input)); Assert.Empty(await central.PuxarEntradasAsync("A", next, default));
        central.Pulso("A", "A", Dados with { ControleAtivo = false }); Assert.Throws<InvalidOperationException>(() => central.Controlar(id, input with { Sessao = next }));
    }
    [Fact] public void FilaLimitadaEGeometriaPrecisamCorresponderAoQuadro()
    {
        var central = new CentralAparelhos(); var id = CentralAparelhos.IdToken("A"); central.Pulso("A", "A", Dados); var session = central.IniciarEspelho("A"); central.Receber("A", session, Jpeg());
        var input = new EntradaCelular(session, "tocar", 240, 480);
        Assert.Throws<InvalidOperationException>(() => central.Controlar(id, input with { Largura = 480 }));
        for (var i = 0; i < 24; i++) central.Controlar(id, input);
        Assert.Throws<InvalidOperationException>(() => central.Controlar(id, input));
    }
    [Fact] public void EnderecoDiretoAceitaIpv6PublicoEPreservaHttpsPinning()
    {
        Assert.True(RedeDireta.Publico(IPAddress.Parse("2804:14c::1000"))); Assert.False(RedeDireta.Publico(IPAddress.Parse("fd7a:115c::1"))); Assert.False(RedeDireta.Publico(IPAddress.Parse("100.110.47.61"))); Assert.False(RedeDireta.Publico(IPAddress.Parse("2001:db8::1")));
        var endpoints = RedeDireta.Filtrar(["https://100.110.47.61:5189", "https://pc.ts.net:5189", "https://192.168.0.64:5189", "https://[2804:14c::1000]:5189"]);
        Assert.Equal(2, endpoints.Length); Assert.All(endpoints, u => Assert.NotNull(EnderecosJarvis.Normalizar(u)));
        Assert.True(RedeDireta.MesmaRede(IPAddress.Loopback)); Assert.False(RedeDireta.MesmaRede(IPAddress.Parse("2606:4700:4700::1111")));
    }
    private static int Port() { using var l = new TcpListener(IPAddress.Loopback, 0); l.Start(); return ((IPEndPoint)l.LocalEndpoint).Port; }
    [Fact] public async Task FilaDoAndroidExigeHttpsTokenESessaoDoProprioAparelho()
    {
        using var cert = CertificadoLocal.Gerar("127.0.0.1"); var plain = Port(); var tls = Port(); var central = new CentralAparelhos();
        await using var server = HospedeJarvis.Criar([], plain, tls, cert, central: central); await server.StartAsync(); var devices = server.Services.GetRequiredService<Aparelhos>(); var token = devices.Parear("Teste controle 190 temporário"); var other = devices.Parear("Teste outro 190 temporário");
        try
        {
            using var http = new HttpClient(); Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync($"http://127.0.0.1:{plain}/api/celular/tela/entradas?sessao=" + Guid.NewGuid())).StatusCode);
            using var https = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, c, _, _) => c?.Thumbprint == cert.Thumbprint }) { BaseAddress = new Uri($"https://127.0.0.1:{tls}") };
            Assert.Equal(HttpStatusCode.Unauthorized, (await https.GetAsync("/api/celular/tela/entradas?sessao=" + Guid.NewGuid())).StatusCode);
            central.Pulso(token, "A", Dados); var session = central.IniciarEspelho(token); central.Receber(token, session, Jpeg()); central.Controlar(CentralAparelhos.IdToken(token), new(session, "tocar", 240, 480));
            https.DefaultRequestHeaders.Add("X-Jarvis-Token", other); Assert.False((await https.GetAsync("/api/celular/tela/entradas?sessao=" + session)).IsSuccessStatusCode);
            https.DefaultRequestHeaders.Remove("X-Jarvis-Token"); https.DefaultRequestHeaders.Add("X-Jarvis-Token", token);
            var input = Assert.Single((await https.GetFromJsonAsync<EntradaCelularPendente[]>("/api/celular/tela/entradas?sessao=" + session))!);
            (await https.PostAsJsonAsync("/api/celular/tela/entrada/confirmar", new ConfirmacaoEntradaCelular(input.Id, "ok"))).EnsureSuccessStatusCode();
            Assert.Empty((await https.GetFromJsonAsync<EntradaCelularPendente[]>("/api/celular/tela/entradas?sessao=" + session))!);
            central.PararEspelho(CentralAparelhos.IdToken(token)); Assert.Equal(HttpStatusCode.Conflict, (await https.GetAsync("/api/celular/tela/entradas?sessao=" + session)).StatusCode);
        }
        finally { devices.Remover(token); devices.Remover(other); await server.StopAsync(); }
    }
}
