// JARVIS Desktop — shell: menu lateral arredondado, troca de páginas, feedback sonoro e visual.
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Jarvis.Core;
using Jarvis.Data;
using Jarvis.Desktop.Services;
using Jarvis.Desktop.ViewModels;
using Jarvis.Desktop.Views.Pages;

namespace Jarvis.Desktop;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();
    private DispatcherTimer? _toastTimer;
    private DispatcherTimer? _celularTimer;
    private readonly BancoJarvis _banco = new();

    public MainWindow()
    {
        InitializeComponent();
        _ = new WindowFrameService(this);
        DataContext = _vm;

        AtualizarUsuario();
        ConfiguracaoApp.ConfiguracoesAlteradas += AoAlterarConfiguracao;

        BarraLateral.Navegar += (_, p) => NavegarPara(p);

        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.PaginaAtual))
                AtualizarConteudo(_vm.PaginaAtual);
            if (e.PropertyName == nameof(MainViewModel.EstadoReator))
                RotuloEstado.Text = Rotulo(_vm.EstadoReator);
        };

        AtualizarConteudo(PageType.Nucleo);
        if (Environment.GetCommandLineArgs().Contains("--emails")) NavegarPara(PageType.Emails);
        IniciarVoz();
        IniciarMonitorCelular();
    }

    private void AtualizarUsuario()
    {
        _vm.ModoEconomico = ConfiguracaoApp.ModoEconomico;
        TxtNomeTop.Text = ConfiguracaoApp.NomeUsuario;
        TxtIniciaisTop.Text = ConfiguracaoApp.IniciaisUsuario;
    }

    private void IniciarMonitorCelular()
    {
        _celularTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _celularTimer.Tick += (_, _) => AtualizarStatusCelular();
        _celularTimer.Start();
        AtualizarStatusCelular();
    }

    private void AtualizarStatusCelular()
    {
        try
        {
            var celular = ServicosAplicacao.CelularOnline;
            if (celular is not null)
            {
                TxtStatusCelular.Text = $"{celular} conectado";
                DotStatus.Fill = (Brush)Application.Current.Resources["CorVerde"];
                TxtStatusCelular.Foreground = (Brush)Application.Current.Resources["CorVerde"];
            }
            else
            {
                TxtStatusCelular.Text = "Celular desconectado";
                DotStatus.Fill = (Brush)Application.Current.Resources["TextoNevoa"];
                TxtStatusCelular.Foreground = (Brush)Application.Current.Resources["TextoGelo"];
            }
        }
        catch { }
    }

    private void AtualizarConteudo(PageType pagina)
    {
        BarraLateral.PaginaAtual = pagina;
        TemaDesktop.Aplicar(BarraLateral);

        AreaConteudo.Content = FabricaPaginas.Criar(
            pagina,
            navegar: NavegarPara,
            toast: MostrarToast,
            falar: FalarTexto,
            ativarEscuta: () => _vm.AtivarEscutaCommand.Execute(null),
            executar: texto => _motor?.ExecutarComandoAsync(texto, false) ?? Task.FromResult("A voz ainda está iniciando. Aguarde um instante.")
        );
        if (!ConfiguracaoApp.ReduzirAnimacoes && !ConfiguracaoApp.ModoEconomico)
            AreaConteudo.BeginAnimation(OpacityProperty, new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160)));
    }

    public void NavegarPara(PageType pagina)
    {
        _vm.PaginaAtual = pagina;
    }

    public void MostrarToast(string mensagem)
    {
        Dispatcher.Invoke(() =>
        {
            ToastTexto.Text = mensagem;
            ToastContainer.Visibility = Visibility.Visible;
            _toastTimer?.Stop();
            _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.5) };
            _toastTimer.Tick += (_, _) =>
            {
                ToastContainer.Visibility = Visibility.Collapsed;
                _toastTimer.Stop();
            };
            _toastTimer.Start();
        });
    }

    public void FalarTexto(string texto)
    {
        _motor?.Falar(texto);
    }

    private void DockReator_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _vm.AtivarEscutaCommand.Execute(null);
    }
    private void Interromper_Click(object sender, RoutedEventArgs e) => _motor?.Interromper();
    private void Minimizar_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximizar_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void Fechar_Click(object sender, RoutedEventArgs e) { WindowState = WindowState.Minimized; Hide(); }
    public void DefinirEscuta(bool ligada) => _motor?.DefinirEscuta(ligada);
    private void AtivarMicrofone_Click(object sender, RoutedEventArgs e) => _vm.AtivarEscutaCommand.Execute(null);

    private MotorVoz? _motor;
    private DispatcherTimer? _medidor;
    private DispatcherTimer? _fps;

    private static string LocalizarModelos()
    {
        var dir = new System.IO.DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir is not null)
        {
            var cand = System.IO.Path.Combine(dir.FullName, "models");
            if (System.IO.File.Exists(System.IO.Path.Combine(cand, "piper", "piper.exe")))
                return cand;
            dir = dir.Parent;
        }
        return System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Jarvis", "models");
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint mod, uint vk);
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(nint hWnd, int id);

    private readonly CancellationTokenSource _vidaJanela = new();
    private Task? _inicioVoz;
    private bool _fechando;
    private void AoAlterarConfiguracao(object? sender, EventArgs e) => Dispatcher.InvokeAsync(AtualizarUsuario);
    private void IniciarVoz()
    {
        _motor = new MotorVoz(LocalizarModelos(), ServicosAplicacao.Email, ServicosAplicacao.Midia, ServicosAplicacao.Apps);
        _motor.Transcricao += (_, t) => { if (!_fechando) Dispatcher.InvokeAsync(() => _vm.UltimaTranscricao = t); };
        _motor.EstadoMudou += (_, e) => { if (!_fechando) Dispatcher.InvokeAsync(() => _vm.EstadoReator = e); };
        _motor.SolicitouNavegacao += (_, p) => { if (!_fechando) Dispatcher.InvokeAsync(() => NavegarPara(p)); };
        _motor.RespostaPronta += (_, t) => { if (!_fechando) Dispatcher.InvokeAsync(() => _vm.UltimaTranscricao = t); };
        _vm.PedidoEscutaTecla += (_, _) => _motor.AtivarEscutaPorTecla();
        Loaded += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(hwnd)?.AddHook(HotkeyHook);
            RegisterHotKey(hwnd, 1, 0x4002, 0x20);
            _medidor = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _medidor.Tick += (_, _) => {
                _vm.NivelAudio = _motor.NivelAudio;
                _medidor.Interval = TimeSpan.FromMilliseconds(_vm.EstadoReator == ReactorState.Falando && !ConfiguracaoApp.ModoEconomico ? 33 : 100);
            };
            _medidor.Start();
            _fps = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _fps.Tick += (_, _) => _vm.FpsTexto = $"{DockReator.RenderizadorAtivo} · {DockReator.FpsMedido:0} fps";
            _fps.Start();
            _inicioVoz = InicializarServicosAsync();
        };
        Closed += (_, _) =>
        {
            MotorVoz.RegistrarVoz("encerramento", "janela");
            _fechando = true; _vidaJanela.Cancel(); _motor.PararFala();
            UnregisterHotKey(new WindowInteropHelper(this).Handle, 1);
            _medidor?.Stop(); _fps?.Stop(); _toastTimer?.Stop(); _celularTimer?.Stop();
            ConfiguracaoApp.ConfiguracoesAlteradas -= AoAlterarConfiguracao;
            if (_inicioVoz is { IsCompleted: false }) _ = _inicioVoz.ContinueWith(_ => _motor.Dispose());
            else _motor.Dispose();
            MotorVoz.RegistrarVoz("encerramento", "voz liberada");
            _banco.Dispose();
        };
    }
    private async Task InicializarServicosAsync()
    {
        try
        {
            _vm.UltimaTranscricao = "Preparando o assistente…";
            if (Environment.GetCommandLineArgs().Contains("--startup")) await Task.Delay(6000, _vidaJanela.Token);
            for (var tentativa = 0; tentativa < 3; tentativa++)
            {
                _vidaJanela.Token.ThrowIfCancellationRequested();
                try { await Task.Run(() => _motor!.Iniciar(), _vidaJanela.Token); _vm.UltimaTranscricao = "Pronto. Diga Jarvis ou pressione Ctrl + Espaço."; break; }
                catch (Exception) when (tentativa < 2) { await Task.Delay(2500, _vidaJanela.Token); }
                catch (Exception) { _vm.UltimaTranscricao = "Microfone indisponível. Confira o dispositivo em Configurações. Os controles e a leitura de e-mails continuam disponíveis."; }
            }
            if (!_fechando) await _motor!.FalarAsync(PersonalizacaoVoz.Saudacao(ConfiguracaoApp.NomeUsuario, DateTime.Now.Hour));
            if (!_fechando && ConfiguracaoApp.LerEmailsAoAbrir && ServicosAplicacao.Email.Conectado)
            {
                try
                {
                    var caixa = await ServicosAplicacao.Email.SincronizarAsync(_vidaJanela.Token);
                    await _motor!.FalarAsync(ResumoCaixaEntrada.ParaFala(caixa), guardarCache: false);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { if (!_fechando) MostrarToast("E-mails: " + ex.Message); }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { MotorVoz.RegistrarVoz("inicio-erro", ex.GetType().Name); }
    }
    private nint HotkeyHook(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        const int WM_HOTKEY = 0x0312;
        if (msg == WM_HOTKEY)
        {
            _vm.AtivarEscutaCommand.Execute(null);
            handled = true;
        }
        return nint.Zero;
    }

    private static string Rotulo(ReactorState e) => e switch
    {
        ReactorState.Repouso => "Diga Jarvis. Estou ouvindo.",
        ReactorState.Ouvindo => "Ouvindo...",
        ReactorState.Pensando => "Pensando...",
        ReactorState.Falando => "Falando...",
        ReactorState.Alerta => "Alerta",
        ReactorState.Mudo => "Mudo",
        ReactorState.Economico => "Econômico",
        _ => "Em repouso",
    };
}
