using Jarvis.Ai;
using Jarvis.Core;
using Jarvis.Sistema;
namespace Jarvis.Desktop.Services;
using System.IO;

/// <summary>Composição das integrações. Views recebem somente contratos Core.</summary>
public static class ServicosAplicacao
{
    public static IHistoricoComandos Historico { get; } = new Jarvis.Data.HistoricoComandos();
    public static IConversaIa ConversaIa { get; } = new Jarvis.Ai.ConversaIaService();
    public static IPreparacaoVoz PreparacaoVoz { get; } = new Jarvis.Voice.PreparacaoVozService();
    public static IPerfMonitor CriarMonitor() => new PerfMonitor();
    public static IEmailService Email { get; } = new EmailImapService();
    public static IAgendaService Agenda { get; } = new Jarvis.Data.AgendaService();
    public static IAcessoExterno AcessoExterno { get; } = new AcessoExternoService();
    public static IMediaController Midia { get; } = new MediaController();
    public static TelemetriaService Telemetria { get; } = new();
    public static IDiagnosticoService Diagnostico { get; } = new DiagnosticoService();
    public static IRotinaService Rotinas { get; } = new RotinaService();
    public static ICatalogoRotinas CatalogoRotinas { get; } = new Jarvis.Data.CatalogoRotinas();
    public static IAppController Apps { get; } = new AppController();
    public static IWindowController Janelas { get; } = new WindowController();
    public static ITransmissaoTela Tela { get; } = new TransmissaoTela();
    public static ICentralAparelhos Aparelhos { get; } = new Jarvis.Server.CentralAparelhos();
    public static IEntradaRemota Entrada { get; } = new EntradaRemota(Tela);
    public static string? CelularOnline => Jarvis.Server.PresencaRemota.NomeOnline;
    public static EstadoCelular? EstadoCelular => Jarvis.Server.PresencaRemota.Estado;
    public static IAudioDispositivos Audio { get; } = new AudioDispositivos();
    public static IInstrumentacaoJogo InstrumentacaoJogo { get; } = new InstrumentacaoJogoService();
    public static IModoJogo Jogo { get; } = new ModoJogoService(instrumentacao: InstrumentacaoJogo);
    public static IConexaoCelular ConexaoCelular { get; } = new ConexaoCelularService();
    public static string ArquivoApk => Path.Combine(AppContext.BaseDirectory, "mobile", "Jarvis.apk");
}
internal sealed class AudioDispositivos : IAudioDispositivos { public IReadOnlyList<string> ListarMicrofones() => Jarvis.Voice.VoiceService.ListarMicrofones(); }
