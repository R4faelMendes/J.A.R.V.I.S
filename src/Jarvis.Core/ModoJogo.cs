namespace Jarvis.Core;

public sealed record AppSegundoPlano(int Id, string Nome);
public sealed record ResultadoModoJogo(bool Ativo, string Mensagem);
public interface IModoJogo
{
    bool Ativo { get; }
    IReadOnlyList<AppSegundoPlano> ListarCandidatos();
    Task<ResultadoModoJogo> AtivarAsync(IReadOnlyCollection<int> processos);
    Task<ResultadoModoJogo> DesativarAsync();
}
