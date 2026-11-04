// JARVIS — contratos. A UI só fala com estas interfaces, nunca com Win32/Google/IA.
namespace Jarvis.Core;

/// <summary>Renderizador do reator (SkiaSharp ou DrawingContext, medidos).</summary>
public interface IReactorRenderer
{
    string Nome { get; }
    void Render(object superficie, ReactorState estado, double nivelAudio, double tempoSeg, int largura, int altura);
}

/// <summary>Voz: captação, VAD, wake word, STT e TTS (fase 2).</summary>
public interface IVoiceService
{
    event EventHandler<string>? TranscricaoParcial;
    event EventHandler? OuvindoMudou;
    bool MicrofoneLigado { get; }
    double NivelAudio { get; }
    void AtivarEscuta();
    void DefinirMicrofoneLigado(bool ligado);
}

/// <summary>Apps e processos: abrir/fechar, catálogo com apelidos (fase 3).</summary>
public interface IAppController
{
    IReadOnlyList<AppInfo> ListarApps();
    void Abrir(string id);
    void Fechar(string id);
    void AdicionarApp(string nome, string executavel, string apelidoVoz, bool ehJogo = false);
    IReadOnlyList<AppInfo> ListarDisponiveis() => ListarApps();
    void SelecionarApps(IReadOnlyCollection<string> ids) => throw new NotSupportedException("Seleção indisponível.");
}

/// <summary>Janelas: listar, minimizar/maximizar/topo/layouts (fase 3).</summary>
public interface IWindowController
{
    IReadOnlyList<WindowInfo> ListarJanelas();
    void Minimizar(nint handle);
    void Maximizar(nint handle);
    void Fechar(nint handle);
    void TrazerParaFrente(nint handle);
    void AplicarLayout(string modelo);
    IReadOnlyList<MonitorInfo> ListarMonitores() => Array.Empty<MonitorInfo>();
    ResultadoLayout Organizar(PedidoLayout pedido) { AplicarLayout(pedido.Modelo); return new(0, Array.Empty<string>()); }
}

/// <summary>Desempenho real do PC (fase 4).</summary>
public interface IPerfMonitor
{
    double CpuPercent { get; }
    double MemoriaPercent { get; }
    ulong MemoriaTotalBytes => 0;
    ulong MemoriaUsadaBytes => 0;
    double GpuPercent { get; }
    double DiscoPercent { get; }
    double CpuPropria { get; }
    double TemperaturaC { get; }
    IReadOnlyList<ProcessoInfo> TopProcessos();
    void EncerrarProcesso(int id);
}

public sealed record ProcessoInfo(int Id, string Nome, double CpuPercent, double MemoriaMB);

/// <summary>IA gratuita com fallback e cota diária (fase 5).</summary>
public interface IChatProvider
{
    string Nome { get; }
    Task<string> PerguntarAsync(string texto, CancellationToken ct = default);
}

public sealed record AppInfo(string Id, string Nome, string ApelidoVoz, bool Aberto, bool EhJogo, string Icone = "⚡");

/// <summary>Abas do navegador via DevTools Protocol (Chrome/Edge com remote-debugging).</summary>
public interface IBrowserController
{
    IReadOnlyList<AbaInfo> ListarAbas();
    void AbrirEndereco(string url);
    void IrParaAba(string id);
    void FecharAba(string id);
}

public sealed record AbaInfo(string Id, string Titulo, string Url);
public sealed record WindowInfo(nint Handle, string Titulo, string App);
