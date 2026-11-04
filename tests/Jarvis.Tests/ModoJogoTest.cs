using Jarvis.Core;
using Jarvis.Sistema;
namespace Jarvis.Tests;

public sealed class ModoJogoTest
{
    private sealed class Energia : IPlanoEnergiaJogo {
        public bool Permitir = true, Restauracao = true;
        public int Ativacoes, Restauracoes;
        public bool Ativar() { Ativacoes++; return Permitir; }
        public bool Restaurar() { Restauracoes++; return Restauracao; }
    }
    [Fact]
    public async Task AtivacaoEconomizaJarvisERestauraPreferenciaAnterior()
    {
        var anterior = ConfiguracaoApp.ModoEconomico;
        var energia = new Energia(); var jogo = new ModoJogoService(energia);
        try {
            ConfiguracaoApp.ModoEconomico = false;
            Assert.True((await jogo.AtivarAsync(Array.Empty<int>())).Ativo);
            Assert.True(ConfiguracaoApp.ModoEconomico);
            await jogo.AtivarAsync(Array.Empty<int>()); Assert.Equal(1, energia.Ativacoes);
            energia.Restauracao = false;
            Assert.True((await jogo.DesativarAsync()).Ativo); Assert.True(ConfiguracaoApp.ModoEconomico);
            energia.Restauracao = true;
            Assert.False((await jogo.DesativarAsync()).Ativo); Assert.False(ConfiguracaoApp.ModoEconomico);
            Assert.Equal(2, energia.Restauracoes);
        } finally { if (jogo.Ativo) { energia.Restauracao = true; await jogo.DesativarAsync(); } ConfiguracaoApp.ModoEconomico = anterior; ConfiguracaoApp.NotificarAlteracao(); }
    }
    [Fact]
    public async Task ProcessoNaoAutorizadoNaoMudaPlanoNemFechaApps()
    {
        var energia = new Energia(); var jogo = new ModoJogoService(energia);
        Assert.False((await jogo.AtivarAsync(new[] { Environment.ProcessId, -1 })).Ativo);
        Assert.Equal(0, energia.Ativacoes);
        Assert.DoesNotContain(jogo.ListarCandidatos(), a => a.Id == Environment.ProcessId);
    }
    [Fact]
    public async Task PlanoIndisponivelNaoAtivaModo()
    {
        var energia = new Energia { Permitir = false }; var jogo = new ModoJogoService(energia);
        var anterior = ConfiguracaoApp.ModoEconomico;
        Assert.False((await jogo.AtivarAsync(Array.Empty<int>())).Ativo);
        Assert.Equal(anterior, ConfiguracaoApp.ModoEconomico);
    }
}
