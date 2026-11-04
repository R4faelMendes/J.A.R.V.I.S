using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using Jarvis.Core;
using Jarvis.Server;
using Microsoft.Extensions.DependencyInjection;
namespace Jarvis.Tests;
public sealed class ControleRemotoTest
{
    private sealed class PcFake : IControleRemoto
    {
        public int Executados;
        public IReadOnlyList<AppInfo> Apps() => new[] { new AppInfo("calc", "Calculadora", "calculadora", false, false) };
        public Task<string> ExecutarAsync(string comando, string? id, bool confirmado)
        { Executados++; return Task.FromResult($"{comando}:{id}:{confirmado}"); }
    }
    private static int Porta()
    {
        using var s = new TcpListener(IPAddress.Loopback, 0); s.Start();
        return ((IPEndPoint)s.LocalEndpoint).Port;
    }
    [Fact]
    public async Task QrApiEComandosExigemHttpsEToken()
    {
        var pc = new PcFake(); var p1 = Porta(); var p2 = Porta();
        using var cert = CertificadoLocal.Gerar("127.0.0.1");
        await using var app = HospedeJarvis.Criar(Array.Empty<string>(), p1, p2, cert, pc);
        await app.StartAsync();
        var aparelhos = app.Services.GetRequiredService<Aparelhos>();
        string? token = null;
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{p1}") };
            Assert.Contains("Baixar Jarvis", await http.GetStringAsync("/mobile"));
            Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync("/api/apps")).StatusCode);
            using var https = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, c, _, _) => c?.Thumbprint == cert.Thumbprint }) { BaseAddress = new Uri($"https://127.0.0.1:{p2}") };
            Assert.Equal(HttpStatusCode.Unauthorized, (await https.GetAsync("/api/apps")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await https.GetAsync("/api/identidade")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await https.GetAsync("/api/telemetria")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await https.GetAsync("/api/janelas")).StatusCode);
            var par = app.Services.GetRequiredService<PareamentoService>();
            par.EmitirPar(out var codigo, out var unico);
            using var resposta = await https.PostAsJsonAsync("/api/parear/confirmar", new { token = unico, nome = "Teste temporário" });
            resposta.EnsureSuccessStatusCode();
            token = (await resposta.Content.ReadFromJsonAsync<Dictionary<string,string>>())!["token"];
            Assert.False(par.ValidarCodigo(codigo, out _));
            Assert.Equal(HttpStatusCode.Unauthorized, (await https.PostAsJsonAsync("/api/parear/confirmar", new { token = unico })).StatusCode);
            https.DefaultRequestHeaders.Add("X-Jarvis-Token", token);
            Assert.Contains("Calculadora", await https.GetStringAsync("/api/apps"));
            Assert.NotNull(await https.GetFromJsonAsync<IdentidadePc>("/api/identidade"));
            Assert.NotNull(await https.GetFromJsonAsync<TelemetriaPc>("/api/telemetria"));
            var cmd = await https.PostAsJsonAsync("/api/comando", new PedidoComando("abrir_app", "calc", true));
            Assert.Contains("abrir_app:calc:True", await cmd.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.BadRequest, (await https.PostAsJsonAsync("/api/comando", new PedidoComando("executar_texto", "cmd"))).StatusCode);
            Assert.Equal(1, pc.Executados);
        }
        finally { if (token is not null) aparelhos.Remover(token); await app.StopAsync(); }
    }
}
