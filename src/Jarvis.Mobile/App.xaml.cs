namespace Jarvis.Mobile;

public partial class App : Application
{
    public static event Action<bool>? AtividadeMudou;
    public static Jarvis.Mobile.Services.ConexaoPc Conexao { get; } = new();
    public static Jarvis.Mobile.Services.AgendaLocal Agenda { get; } = new();
    public static string? PaginaPendente { get; set; }
    public static bool EmPrimeiroPlano { get; private set; } = true;

    private static string? _link;
    public static string? LinkPareamento
    {
        get => _link;
        set { _link = value; if (value is not null && _iniciou) MainThread.BeginInvokeOnMainThread(async () => { try { if (Shell.Current is { } shell && !(shell.CurrentState?.Location?.ToString().Contains("parear") ?? false)) await shell.GoToAsync("parear"); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); } }); }
    }
    public App()
    {
        InitializeComponent();
        Services.TemaMobile.Iniciar();
        Services.DiagnosticoMobile.Iniciar();
        MainPage = new AppShell();
        MainPage.DescendantAdded += (_, e) => { if (e.Element is VisualElement v) v.Loaded += (_, _) => Services.TemaMobile.Aplicar(v); };
        MainPage.Loaded += (_, _) => Services.TemaMobile.Aplicar(MainPage);
        Conexao.EstadoMudou += (_, _) => { if (Conexao.Conectado) MainThread.BeginInvokeOnMainThread(Services.DispositivoCompanheiro.Restaurar); };
#if ANDROID
        string? widgetPair = null;
        Conexao.EstadoMudou += (_, _) => { var pair = Conexao.IdentificadorPareamento; if (pair != widgetPair) { widgetPair = pair; JarvisWidget.PublicarAgenda(Agenda.Listar()); } };
#endif
        MainPage.Loaded += async (_, _) => await IniciarAsync();
    }
    private static bool _iniciou;
    private static async Task IniciarAsync()
    {
        if (_iniciou) return; _iniciou = true;
        try
        {
            await Conexao.CarregarAsync(); Conexao.IniciarMonitor();
            Services.Notificacoes.Reagendar(Agenda.Listar()); Services.MidiaNotificacao.Restaurar(); Services.DispositivoCompanheiro.Restaurar();
            if (PaginaPendente is { } destino) { PaginaPendente = null; await Shell.Current.GoToAsync(destino); return; }
            if (App.LinkPareamento is not null || Conexao.BaseUrl is null)
            {
                var shell = Shell.Current;
                if (shell is not null && !(shell.CurrentState?.Location?.ToString().Contains("parear") ?? false)) await shell.GoToAsync("parear");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Inicialização Jarvis: " + ex);
            // Falha na restauração de uma conexão não deve derrubar o aplicativo.
        }
    }
    protected override void OnSleep() { EmPrimeiroPlano = false; AtividadeMudou?.Invoke(false); if (!Services.MidiaNotificacao.EmExecucao && !Services.DispositivoCompanheiro.Ativo && !Services.EspelhoCelular.Ativo) Conexao.PararMonitor(); Views.ReatorView.DefinirAppAtivo(false); Views.ReactorArt.DefinirAppAtivo(false); base.OnSleep(); }
    protected override void OnResume() { EmPrimeiroPlano = true; AtividadeMudou?.Invoke(true); Conexao.IniciarMonitor(); Services.DispositivoCompanheiro.Restaurar(); Views.ReatorView.DefinirAppAtivo(true); Views.ReactorArt.DefinirAppAtivo(true); base.OnResume(); }
}
