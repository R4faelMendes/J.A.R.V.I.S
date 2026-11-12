using Jarvis.Core;
namespace Jarvis.Data;
public sealed class CatalogoRotinas : ICatalogoRotinas
{
    public IReadOnlyList<RotinaDefinida> Listar()
    {
        using var banco = new BancoJarvis();
        return banco.ListarRotinasCompletas().Select(r => new RotinaDefinida(r.Id, r.Nome, r.Gatilho, r.Acoes, r.Ativa)).ToArray();
    }
    public void Adicionar(string nome, string gatilho, string acoes)
    {
        PlanoRotina.Validar(acoes);
        using var banco = new BancoJarvis(); banco.InserirRotina(nome, "Voz: " + gatilho, gatilho, acoes);
    }
    public void Alternar(long id, bool ativa) { using var banco = new BancoJarvis(); banco.AlternarRotina(id, ativa); }
    public void Atualizar(long id, string nome, string gatilho, string acoes)
    {
        PlanoRotina.Validar(acoes);
        using var banco = new BancoJarvis(); banco.AtualizarRotina(id, nome, "Voz: " + gatilho, gatilho, acoes);
    }
    public void Remover(long id) { using var banco = new BancoJarvis(); banco.RemoverRotina(id); }
}
