namespace Jarvis.Core;

public sealed record RecursoVoz(string Nome, bool Pronto, string Detalhe);
public sealed record ProgressoDownload(string Etapa, long Bytes, long? Total = null);
public interface IPreparacaoVoz
{
    string Pasta { get; }
    IReadOnlyList<RecursoVoz> Verificar();
    Task InstalarAsync(IProgress<ProgressoDownload> progresso, CancellationToken ct);
    Task<string> TestarAsync(CancellationToken ct);
    Task<string> TestarMicrofoneAsync(CancellationToken ct);
}

public sealed record RegistroComando(long Id, string Quando, string Comando, string Resposta, string Origem);
public interface IHistoricoComandos
{
    event EventHandler? Alterado;
    IReadOnlyList<RegistroComando> Listar(int limite = 50);
    void Registrar(string comando, string resposta, string origem);
    void Limpar();
}
public sealed record ComandoProcessado(string Comando, string Resposta, string Origem);

public sealed record PreferenciasIa(bool Ativa = false, string Gemini = "gemini-3.5-flash-lite", string Groq = "openai/gpt-oss-20b", string OpenRouter = "openrouter/free");
public sealed record RespostaIa(string Texto, string Provedor, bool Online);
public interface IConversaIa
{
    PreferenciasIa Preferencias { get; }
    IReadOnlyList<string> ProvedoresConfigurados { get; }
    int UsadasHoje { get; }
    void Configurar(PreferenciasIa preferencias, string provedor, string? chave, bool removerChave = false);
    Task<RespostaIa> PerguntarAsync(string pergunta, CancellationToken ct);
}
public interface IResumoDia { Task<string> GerarAsync(CancellationToken ct); }

public sealed record AgendamentoRotina(long RotinaId, string Hora, DayOfWeek[] Dias, bool Ativo, string? UltimaExecucao = null);
public interface IAgendadorRotinas
{
    IReadOnlyList<AgendamentoRotina> Listar();
    void Salvar(AgendamentoRotina item);
    void Remover(long id);
    string UltimoResultado { get; }
    void Iniciar();
    void Parar();
}

public sealed record VersaoDisponivel(string Versao, string Notas, Uri Pagina, Uri Executavel, Uri Checksums);
public interface IAtualizadorJarvis
{
    string VersaoAtual { get; }
    Task<VersaoDisponivel?> ConsultarAsync(CancellationToken ct);
    Task<string> BaixarAsync(VersaoDisponivel versao, IProgress<ProgressoDownload> progresso, CancellationToken ct);
    Task InstalarAoSairAsync(string arquivo, CancellationToken ct);
}
