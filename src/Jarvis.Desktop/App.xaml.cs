using System.IO;
using System.Windows;
using Jarvis.Desktop.Views;
namespace Jarvis.Desktop;

public partial class App : Application
{
    private TrayService? _bandeja;
    private MiniOverlay? _mini;
    private Services.ServidorLocal? _servidor;
    private Mutex? _instancia;
    private bool _donoMutex;
    protected override void OnStartup(StartupEventArgs e)
    {
        _instancia = new Mutex(true, @"Local\Jarvis.Desktop", out _donoMutex);
        if (!_donoMutex) { Shutdown(); return; }
        base.OnStartup(e);
        Services.TemaDesktop.Iniciar();
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        DispatcherUnhandledException += (_, erro) => RegistrarFalha(erro.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, erro) => RegistrarFalha(erro.ExceptionObject as Exception);
        var principal = new MainWindow();
        MainWindow = principal;
        _bandeja = new TrayService();
        _bandeja.AoAbrir += (_, _) => { principal.Show(); principal.WindowState = WindowState.Normal; principal.Activate(); };
        _bandeja.AoSilenciar += (_, mudo) => principal.DefinirEscuta(!mudo);
        principal.Show();
        _mini = new MiniOverlay { Owner = principal };
        _servidor = new Services.ServidorLocal();
        _servidor.Iniciar();
        Services.ServicosAplicacao.Agendador.Iniciar();
    }
    private static void RegistrarFalha(Exception? ex)
    {
        try
        {
            var pasta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis");
            Directory.CreateDirectory(pasta);
            File.WriteAllText(Path.Combine(pasta, "ultima-falha.log"), $"{DateTimeOffset.Now:O}\n{ex?.GetType().Name}\n{ex?.StackTrace}");
        }
        catch { }
    }
    protected override void OnExit(ExitEventArgs e)
    {
        Services.ServicosAplicacao.Agendador.Parar();
        if (Services.ServicosAplicacao.Jogo.Ativo) Services.ServicosAplicacao.Jogo.DesativarAsync().GetAwaiter().GetResult();
        Services.ServicosAplicacao.InstrumentacaoJogo.Dispose();
        Services.MotorVoz.RegistrarVoz("encerramento", "servicos");
        _mini?.Close(); _servidor?.Dispose(); _bandeja?.Dispose(); Services.ServicosAplicacao.Telemetria.Dispose();
        Services.MotorVoz.RegistrarVoz("encerramento", "concluido");
        if (_donoMutex) _instancia?.ReleaseMutex();
        _instancia?.Dispose();
        base.OnExit(e);
    }
}
