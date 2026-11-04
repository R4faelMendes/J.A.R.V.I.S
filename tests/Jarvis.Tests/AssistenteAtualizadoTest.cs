using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Jarvis.Ai;
using Jarvis.Core;
using Jarvis.Voice;

namespace Jarvis.Tests;

public sealed class AssistenteAtualizadoTest
{
    [Theory]
    [InlineData("Jarvis, próxima música por favor", "media_next")]
    [InlineData("pause a música", "media_pause")]
    [InlineData("continuar a música", "media_play")]
    [InlineData("música anterior", "media_previous")]
    [InlineData("que música está tocando?", "media_status")]
    [InlineData("leia meus e-mails", "read_inbox")]
    [InlineData("Abra a calculadora", "open_app")]
    [InlineData("confirmar", "confirm")]
    public void ComandosNaturais_DisparamAcoes(string fala, string acao)
    {
        var r = RoteadorIntencoes.Rotear(fala);
        Assert.Equal(acao, r?.Acao);
        Assert.Null(r?.Resposta); // Não anunciar sucesso antes da execução.
    }
    [Theory]
    [InlineData("quando é a próxima reunião?")]
    [InlineData("explique o que é uma pausa musical")]
    public void Perguntas_NaoDisparamMidia(string fala) => Assert.Null(RoteadorIntencoes.Rotear(fala));
    [Fact]
    public void VelocidadePiper_IndependeDaCultura()
    {
        var antes = CultureInfo.CurrentCulture;
        try { CultureInfo.CurrentCulture = new("pt-BR"); Assert.Equal("0.909", PiperTts.Escala(1.1)); }
        finally { CultureInfo.CurrentCulture = antes; }
    }
    [Fact]
    public void Gmail_LerMetadadosEOrdenarResumoSemCorpo()
    {
        using var doc = JsonDocument.Parse("""{"id":"1","internalDate":"1700000000000","labelIds":["INBOX","UNREAD"],"snippet":"A &amp; B","payload":{"headers":[{"name":"From","value":"Ana <ana@example.test>"},{"name":"Subject","value":"Reunião"}]}}""");
        var mail = GoogleMailService.LerMensagem(doc.RootElement);
        Assert.Equal("Ana", mail.Remetente); Assert.True(mail.NaoLido); Assert.Equal("A & B", mail.Previa);
        var fala = ResumoCaixaEntrada.ParaFala(new(new[] { mail }, 1, 1));
        Assert.Contains("Reunião", fala); Assert.DoesNotContain("A & B", fala);
        Assert.Equal("Sua caixa de entrada está vazia.", ResumoCaixaEntrada.ParaFala(new(Array.Empty<EmailResumo>(), 0, 0)));
    }
    [Fact]
    public async Task Google_LoginPkceRenovacaoECofre_SemContaReal()
    {
        var pasta = Path.Combine(Path.GetTempPath(), "Jarvis-oauth-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(pasta);
        try
        {
            var json = Path.Combine(pasta, "cliente.json");
            await File.WriteAllTextAsync(json, """{"installed":{"client_id":"teste.apps.googleusercontent.com","client_secret":"secret-teste"}}""");
            var handler = new GoogleFake();
            using var http = new HttpClient(handler);
            Task? callback = null;
            var servico = new GoogleMailService(pasta, http, url =>
            {
                var query = Query(url.Query);
                Assert.Equal("S256", query["code_challenge_method"]);
                Assert.Equal("https://www.googleapis.com/auth/gmail.readonly", query["scope"]);
                Assert.Equal(43, query["code_challenge"].Length);
                callback = Task.Run(async () =>
                {
                    using var local = new HttpClient();
                    // Um callback com estado errado não pode concluir o login.
                    var errado = await local.GetAsync(query["redirect_uri"] + "?state=errado&code=invasor");
                    Assert.Equal(HttpStatusCode.BadRequest, errado.StatusCode);
                    var certo = await local.GetAsync(query["redirect_uri"] + "?state=" + query["state"] + "&code=code-teste");
                    Assert.True(certo.IsSuccessStatusCode);
                });
            });
            await servico.ConfigurarClienteAsync(json);
            using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await servico.ConectarAsync(limite.Token);
            await callback!;
            Assert.True(servico.Conectado);
            var bytes = await File.ReadAllBytesAsync(Path.Combine(pasta, "google-mail.dpapi"));
            Assert.DoesNotContain("refresh-teste", Encoding.UTF8.GetString(bytes));
            var reaberto = new GoogleMailService(pasta, http);
            var caixa = await reaberto.SincronizarAsync();
            Assert.Empty(caixa.Mensagens); Assert.Equal(0, caixa.Total);
            Assert.Contains(handler.Corpos, c => c.Contains("grant_type=refresh_token"));
            Assert.Contains(handler.Corpos, c => c.Contains("code_verifier="));
            await reaberto.DesconectarAsync(); Assert.False(reaberto.Conectado);
            Assert.False(new GoogleMailService(pasta, http).Conectado);
        }
        finally { Directory.Delete(pasta, true); }
    }
    private static Dictionary<string, string> Query(string q) => q.TrimStart('?').Split('&').Select(x => x.Split('=', 2)).ToDictionary(x => Uri.UnescapeDataString(x[0]), x => Uri.UnescapeDataString(x[1]));
    private sealed class GoogleFake : HttpMessageHandler
    {
        public List<string> Corpos { get; } = new();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
        {
            if (r.Content is not null) Corpos.Add(await r.Content.ReadAsStringAsync(ct));
            string texto;
            if (r.RequestUri!.AbsolutePath == "/token") texto = """{"access_token":"access-teste","refresh_token":"refresh-teste","expires_in":0}""";
            else if (r.RequestUri.AbsolutePath == "/revoke") texto = "{}";
            else
            {
                Assert.Equal("Bearer", r.Headers.Authorization?.Scheme);
                Assert.Equal("access-teste", r.Headers.Authorization?.Parameter);
                Assert.Equal(HttpMethod.Get, r.Method);
                texto = r.RequestUri.AbsolutePath.EndsWith("INBOX") ? """{"messagesTotal":0,"messagesUnread":0}""" : """{"messages":[]}""";
            }
            return new(HttpStatusCode.OK) { Content = new StringContent(texto, Encoding.UTF8, "application/json") };
        }
    }
}
