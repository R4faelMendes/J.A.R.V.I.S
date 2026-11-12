using System.Net;
using System.Security.Cryptography;
using System.Text;
using Jarvis.Core;
using Jarvis.Data;
using Jarvis.Desktop.Services;
namespace Jarvis.Tests;

public sealed class Evolucao240Test
{
    private static string Pasta() { var p = Path.Combine(Path.GetTempPath(), "jarvis-240-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(p); return p; }
    private sealed class Catalogo : ICatalogoRotinas
    {
        public IReadOnlyList<RotinaDefinida> Listar() => [new(1, "Estudar", "Voz: estudar", "[{\"Comando\":\"esperar\",\"Argumento\":\"1\"}]", true)];
        public void Adicionar(string a, string b, string c) => throw new NotSupportedException();
        public void Alternar(long id, bool ativa) => throw new NotSupportedException();
    }
    private sealed class Executor : IRotinaService
    {
        public int Chamadas;
        public Task ExecutarAsync(string passos, IProgress<ProgressoRotina> progresso, CancellationToken ct) { ct.ThrowIfCancellationRequested(); Chamadas++; return Task.CompletedTask; }
    }
    [Fact]
    public async Task AgendaExecutaUmaVezPorMinutoMesmoDepoisDeReabrir()
    {
        var pasta = Pasta(); var db = Path.Combine(pasta, "db.sqlite"); var exec = new Executor(); var hist = new HistoricoComandos(db); var cat = new Catalogo();
        var a = new AgendadorRotinas(cat, exec, hist, db); var agora = new DateTimeOffset(2026, 11, 12, 9, 30, 0, TimeSpan.FromHours(-3));
        a.Salvar(new(1, "09:30", [DayOfWeek.Thursday], true));
        await a.ExecutarPendentesAsync(agora.AddMinutes(-1)); Assert.Equal(0, exec.Chamadas);
        await a.ExecutarPendentesAsync(agora); await a.ExecutarPendentesAsync(agora.AddSeconds(20));
        var reaberto = new AgendadorRotinas(cat, exec, hist, db); await reaberto.ExecutarPendentesAsync(agora.AddSeconds(40));
        Assert.Equal(1, exec.Chamadas); Assert.Equal("agendamento", Assert.Single(hist.Listar()).Origem);
        await reaberto.ExecutarPendentesAsync(agora.AddDays(1)); Assert.Equal(1, exec.Chamadas);
        await reaberto.ExecutarPendentesAsync(agora.AddDays(7)); Assert.Equal(2, exec.Chamadas);
        reaberto.Salvar(new(1, "09:30", [DayOfWeek.Thursday], false)); await reaberto.ExecutarPendentesAsync(agora.AddDays(14)); Assert.Equal(2, exec.Chamadas);
    }
    [Theory]
    [InlineData("25:00")]
    [InlineData("9:30")]
    [InlineData("09:30; comando")]
    public void AgendaRecusaHorarioInvalido(string hora) => Assert.Throws<ArgumentException>(() => RegraAgendamento.Validar(new(1, hora, [DayOfWeek.Monday], true)));

    private sealed class HttpFalso(Func<HttpRequestMessage, HttpResponseMessage> resposta) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => Task.FromResult(resposta(request));
    }
    private static VersaoDisponivel Release() => new("9.0.0", "notas", new("https://github.com/R4faelMendes/J.A.R.V.I.S/releases/tag/v9.0.0"), new("https://github.com/R4faelMendes/J.A.R.V.I.S/releases/download/v9.0.0/Jarvis-Windows-x64.exe"), new("https://github.com/R4faelMendes/J.A.R.V.I.S/releases/download/v9.0.0/SHA256SUMS.txt"));
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AtualizacaoVerificaHashEImpedeArquivoAdulterado(bool corrompido)
    {
        var bytes = Encoding.UTF8.GetBytes("executavel de teste"); var hash = Convert.ToHexString(SHA256.HashData(bytes));
        var http = new HttpClient(new HttpFalso(r => new(HttpStatusCode.OK) { Content = r.RequestUri!.AbsolutePath.EndsWith(".txt") ? new StringContent(hash + "  Jarvis-Windows-x64.exe\n") : new ByteArrayContent(corrompido ? [1, 2, 3] : bytes) }));
        var service = new AtualizadorJarvis(http, Pasta(), "2.4.0");
        if (corrompido) { await Assert.ThrowsAsync<InvalidDataException>(() => service.BaixarAsync(Release(), new Progress<ProgressoDownload>(), default)); return; }
        var file = await service.BaixarAsync(Release(), new Progress<ProgressoDownload>(), default); Assert.Equal(bytes, File.ReadAllBytes(file));
        File.WriteAllText(file, "alterado após download"); await Assert.ThrowsAsync<InvalidDataException>(() => service.InstalarAoSairAsync(file, default));
    }
    [Theory]
    [InlineData("http://github.com/R4faelMendes/J.A.R.V.I.S/releases/download/v9.0.0/Jarvis-Windows-x64.exe")]
    [InlineData("https://evil.example/Jarvis-Windows-x64.exe")]
    [InlineData("https://github.com/Outro/Projeto/releases/download/v9.0.0/Jarvis-Windows-x64.exe")]
    public void AtualizadorRecusaFonteNaoOficial(string uri) => Assert.Throws<InvalidDataException>(() => AtualizadorJarvis.ValidarUrl(new(uri), "Jarvis-Windows-x64.exe"));
    [Fact]
    public void AtualizadorRecusaChecksumsAmbiguos() => Assert.Throws<InvalidDataException>(() => AtualizadorJarvis.LerChecksum(new string('a', 64) + "  Jarvis-Windows-x64.exe\n" + new string('b', 64) + "  Jarvis-Windows-x64.exe"));
    private sealed class Agenda : IAgendaService
    {
        public IReadOnlyList<Compromisso> Listar() => [new(Guid.NewGuid().ToString(), "Reunião de hoje", DateTimeOffset.Now, DateTimeOffset.Now.AddMinutes(30)), new(Guid.NewGuid().ToString(), "Já concluído", DateTimeOffset.Now, DateTimeOffset.Now.AddMinutes(30), Etapa: EtapaCompromisso.Concluido)];
        public Compromisso Salvar(Compromisso item) => throw new NotSupportedException();
    }
    private sealed class Email(bool conectado) : IEmailService
    {
        public bool Configurado => conectado;
        public bool Conectado => conectado;
        public int Consultas;
        public Task<CaixaEntrada> SincronizarAsync(CancellationToken ct = default) { ct.ThrowIfCancellationRequested(); Consultas++; return Task.FromResult(new CaixaEntrada([new("1", "Remetente", "Assunto real", "", DateTimeOffset.Now, true)], 1, 1)); }
        public Task ConfigurarClienteAsync(string file) => throw new NotSupportedException();
        public Task ConectarAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task DesconectarAsync(CancellationToken ct = default) => throw new NotSupportedException();
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResumoUsaFontesReaisENaoConsultaContaDesconectada(bool conectado)
    {
        var email = new Email(conectado); var resumo = await new ResumoDiaService(new Agenda(), email).GerarAsync(default);
        Assert.Contains("Reunião de hoje", resumo); Assert.DoesNotContain("Já concluído", resumo); Assert.Contains("Fonte: agenda local", resumo);
        Assert.Equal(conectado ? 1 : 0, email.Consultas);
        if (conectado) Assert.Contains("Assunto real", resumo); else { Assert.Contains("desconectado", resumo); Assert.DoesNotContain("Assunto real", resumo); }
    }
}
