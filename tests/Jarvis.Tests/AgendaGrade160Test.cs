using Jarvis.Core;
using Jarvis.Data;
using Jarvis.Server;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
namespace Jarvis.Tests;
public sealed class AgendaGrade160Test
{
    private static Compromisso Evento()
    {
        var day = DateTime.Today.AddDays(20);
        return new(Guid.NewGuid().ToString(), "Consulta", new DateTimeOffset(day.Year, day.Month, day.Day, 14, 0, 0, TimeSpan.FromHours(-3)), new DateTimeOffset(day.Year, day.Month, day.Day, 15, 0, 0, TimeSpan.FromHours(-3)), 15);
    }
    [Fact] public void LembretePreservaInstanteEFusoSemDependerDeConexao()
    {
        var item = Evento(); item.Validar(); Assert.Equal(item.Inicio.AddMinutes(-15), item.AlertaEm); Assert.Equal(16, item.AlertaEm!.Value.UtcDateTime.Hour);
        Assert.Null((item with { AvisoMinutos = -1 }).AlertaEm); Assert.Null((item with { Excluido = true }).AlertaEm);
        Assert.Throws<ArgumentException>(() => (item with { Fim = item.Inicio }).Validar()); Assert.Throws<ArgumentException>(() => (item with { Titulo = "" }).Validar());
    }
    [Theory] [InlineData("Metade", 1)] [InlineData("Grade2x2", 3)] [InlineData("TresColunas", 2)]
    public void JanelaUnicaPodeOcuparQualquerCelula(string model, int position)
    {
        var area = new RegiaoJanela(-1920, 0, 1920, 1040); var all = GradeJanelas.Calcular(model, area, GradeJanelas.Capacidade(model));
        var single = PosicoesGrade.Calcular(model, area, 1, [position]); Assert.Equal(all[position], Assert.Single(single));
        Assert.Throws<ArgumentException>(() => PosicoesGrade.Calcular(model, area, 2, [position, position]));
        Assert.Throws<ArgumentException>(() => PosicoesGrade.Calcular(model, area, 1, [99]));
    }
    [Fact] public void AgendaPersisteAlteracaoExclusaoERejeitaSobrescrita()
    {
        var path = Path.Combine(Path.GetTempPath(), "jarvis-agenda-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            var service = new AgendaService(path); var initial = service.Salvar(Evento()); Assert.Equal(1, initial.Versao);
            var updated = service.Salvar(initial with { Titulo = "Consulta atualizada" }); Assert.Equal(2, updated.Versao);
            Assert.Throws<ConflitoAgendaException>(() => service.Salvar(initial with { Titulo = "edição antiga" }));
            var removed = service.Salvar(updated with { Excluido = true }); Assert.True(removed.Excluido); Assert.Equal(3, removed.Versao);
            var reopened = new AgendaService(path); Assert.Equal(removed, Assert.Single(reopened.Listar()));
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.Delete(path); }
    }
    private sealed class Pc : IControleRemoto
    {
        private Compromisso? _event;
        public IReadOnlyList<AppInfo> Apps() => [];
        public Task<string> ExecutarAsync(string comando, string? id, bool confirmado) => Task.FromResult("ok");
        public IReadOnlyList<Compromisso> Agenda() => _event is null ? [] : [_event];
        public Compromisso SalvarCompromisso(Compromisso item) { item.Validar(); if ((_event?.Versao ?? 0) != item.Versao) throw new ConflitoAgendaException(); return _event = item with { Versao = item.Versao + 1 }; }
        public Task<EstadoMidia> MidiaAsync() => Task.FromResult(new EstadoMidia(true, "Música de teste", "Artista", "Spotify", true, [1, 2, 3]));
    }
    private static int Port() { using var t = new TcpListener(IPAddress.Loopback, 0); t.Start(); return ((IPEndPoint)t.LocalEndpoint).Port; }
    [Fact] public async Task AgendaECapaExigemPareamentoEConflitoVira409()
    {
        using var cert = CertificadoLocal.Gerar("127.0.0.1"); var plain = Port(); var tls = Port();
        await using var app = HospedeJarvis.Criar([], plain, tls, cert, new Pc()); await app.StartAsync(); var devices = app.Services.GetRequiredService<Aparelhos>(); string? token = null;
        try
        {
            using var https = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, c, _, _) => c?.Thumbprint == cert.Thumbprint }) { BaseAddress = new Uri($"https://127.0.0.1:{tls}") };
            Assert.Equal(HttpStatusCode.Unauthorized, (await https.GetAsync("/api/agenda")).StatusCode); Assert.Equal(HttpStatusCode.Unauthorized, (await https.GetAsync("/api/midia")).StatusCode);
            using var http = new HttpClient(); Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync($"http://127.0.0.1:{plain}/api/agenda")).StatusCode);
            token = devices.Parear("Teste agenda temporário"); https.DefaultRequestHeaders.Add("X-Jarvis-Token", token); var item = Evento();
            using var saved = await https.PostAsJsonAsync("/api/agenda", item); saved.EnsureSuccessStatusCode();
            Assert.Single((await https.GetFromJsonAsync<List<Compromisso>>("/api/agenda"))!); Assert.Equal(HttpStatusCode.Conflict, (await https.PostAsJsonAsync("/api/agenda", item)).StatusCode);
            Assert.Equal(new byte[] { 1, 2, 3 }, (await https.GetFromJsonAsync<EstadoMidia>("/api/midia"))!.Capa);
        }
        finally { if (token is not null) devices.Remover(token); await app.StopAsync(); }
    }
}
