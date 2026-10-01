// <summary>
// Refatoração planejada: abstrações que vivem em Jarvis.Core.
// A UI (WPF/MAUI) nunca fala com Win32, Google ou IA diretamente:
// chama estas interfaces. Cada implementação pode ser trocada e testada.
// </summary>
namespace Jarvis.Core.Abstracoes;

public interface IServicoVoz
{
    EstadoReator EstadoAtual { get; }
    event EventHandler<EstadoReator>? EstadoMudou;
    event EventHandler<string>? TranscricaoParcial;
    Task IniciarEscutaAsync(CancellationToken ct = default);
    Task PararEscutaAsync();
    Task FalarAsync(string texto, CancellationToken ct = default);
}

public enum EstadoReator
{
    Repouso,
    Ouvindo,
    Pensando,
    Falando,
    Alerta,
    Mudo,
    Economico
}

public interface IControladorApps
{
    Task<IReadOnlyList<AppInfo>> ListarAsync(CancellationToken ct = default);
    Task AbrirAsync(string id, CancellationToken ct = default);
    Task FecharAsync(string id, CancellationToken ct = default);
}

public sealed record AppInfo(string Id, string Nome, string? ApelidoVoz, bool Aberto, bool Fixado);

public interface IControladorJanelas
{
    Task<IReadOnlyList<JanelaInfo>> ListarAsync(CancellationToken ct = default);
    Task MinimizarAsync(nint handle);
    Task MaximizarAsync(nint handle);
    Task FixarTopoAsync(nint handle, bool fixar);
    Task MoverParaMonitorAsync(nint handle, int monitor);
    Task AplicarLayoutAsync(ModeloLayout layout);
}

public sealed record JanelaInfo(nint Handle, string Titulo, string App, int Monitor);
public enum ModeloLayout { Metade, TresColunas, FocoLateral, Grade2x2 }

public interface IMonitorDesempenho
{
    event EventHandler<AmostraDesempenho>? Amostra;
    AmostraDesempenho Atual { get; }
    Task<IReadOnlyList<ProcessoInfo>> TopProcessosAsync(int quantidade = 5);
    Task EncerrarProcessoAsync(int pid);
}

public sealed record AmostraDesempenho(
    float Cpu, float Memoria, float Gpu, float Disco,
    float Temperatura, float CpuJarvis, DateTimeOffset Hora);

public sealed record ProcessoInfo(int Pid, string Nome, float Cpu, long MemoriaBytes);

public interface IServicoEmail
{
    Task<IReadOnlyList<EmailResumo>> ListarAsync(bool soImportantes, CancellationToken ct = default);
    Task<EmailDetalhe> LerAsync(string id, CancellationToken ct = default);
    Task<string> ResumirAsync(string id, CancellationToken ct = default);
    Task ArquivarAsync(string id);
}

public sealed record EmailResumo(string Id, string Remetente, string Assunto, string Previa, DateTimeOffset Hora, bool Lido, bool Importante);
public sealed record EmailDetalhe(string Id, string Remetente, string Assunto, string Corpo, DateTimeOffset Hora, string? Resumo);

public interface IServicoAgenda
{
    Task<IReadOnlyList<EventoAgenda>> ListarDiaAsync(DateOnly dia, CancellationToken ct = default);
    Task<EventoAgenda> CriarAsync(NovoEvento evento, CancellationToken ct = default);
    Task ExcluirAsync(string id);
}

public sealed record EventoAgenda(string Id, string Titulo, DateTimeOffset Inicio, DateTimeOffset Fim, string? Local, string? Descricao);
public sealed record NovoEvento(string Titulo, DateTimeOffset Inicio, DateTimeOffset Fim, string? Lembrete, string[] Convidados);

public interface IProvedorChat
{
    string Nome { get; }
    Task<string> ResponderAsync(string mensagem, CancellationToken ct = default);
    Task<int> UsoDoDiaAsync();
}
