using Jarvis.Core;
using Jarvis.Data;
using Jarvis.Ai;
namespace Jarvis.Tests;

public sealed class EvolucaoServicosTest
{
    private static string Pasta() { var path = Path.Combine(Path.GetTempPath(), "jarvis-evolucao-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(path); return path; }
    [Fact]
    public void HistoricoPersisteUltimosComandosSemInterpretarSql()
    {
        var path = Path.Combine(Pasta(), "history.db"); var h = new HistoricoComandos(path);
        h.Registrar("abra 'calculadora'; DROP TABLE comandos;", "Resultado literal", "digitado");
        h.Registrar("hora", "12h", "voz");
        var outraInstancia = new HistoricoComandos(path);
        Assert.Equal("hora", outraInstancia.Listar()[0].Comando);
        Assert.Contains("DROP TABLE", outraInstancia.Listar()[1].Comando);
        h.Limpar(); Assert.Empty(outraInstancia.Listar());
    }
    private sealed class Provedor(string nome, bool falhar = false) : IChatProvider
    {
        public string Nome => nome;
        public int Chamadas;
        public Task<string> PerguntarAsync(string texto, CancellationToken ct = default) { ct.ThrowIfCancellationRequested(); Chamadas++; return falhar ? Task.FromException<string>(new HttpRequestException("offline")) : Task.FromResult("Resposta textual, sem ações."); }
    }
    [Fact]
    public async Task IaSemConsentimentoNaoCarregaProvedoresNemFazRequisicao()
    {
        var criou = false; var ia = new ConversaIaService(Pasta(), _ => { criou = true; return []; });
        var resposta = await ia.PerguntarAsync("explique computação", default);
        Assert.False(criou); Assert.False(resposta.Online); Assert.Equal(0, ia.UsadasHoje);
    }
    [Fact]
    public async Task IaUsaFallbackIdentificaFonteEPersisteLimite()
    {
        var a = new Provedor("indisponível", true); var b = new Provedor("teste"); var pasta = Pasta();
        var ia = new ConversaIaService(pasta, _ => [a, b]); ia.Configurar(new(true), "groq", null);
        var resposta = await ia.PerguntarAsync("pergunta", default);
        Assert.True(resposta.Online); Assert.Equal("teste", resposta.Provedor); Assert.Equal(2, ia.UsadasHoje);
        for (var i = 0; i < 9; i++) await ia.PerguntarAsync("pergunta", default);
        Assert.Equal(20, ia.UsadasHoje);
        resposta = await ia.PerguntarAsync("pergunta excedente", default);
        Assert.False(resposta.Online); Assert.Equal(10, b.Chamadas);
        Assert.Equal(20, new ConversaIaService(pasta).UsadasHoje);
    }
}
