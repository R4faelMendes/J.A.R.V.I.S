// JARVIS Desktop — hospedeiro do reator: timer 30fps, pausa oculto/minimizado, FPS real.
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Jarvis.Core;
using Jarvis.Desktop.Renderers;

namespace Jarvis.Desktop.Views;

public partial class ReactorView : UserControl
{
    public static readonly DependencyProperty EstadoProperty =
        DependencyProperty.Register(nameof(Estado), typeof(ReactorState),
            typeof(ReactorView), new PropertyMetadata(ReactorState.Repouso));

    public static readonly DependencyProperty NivelAudioProperty =
        DependencyProperty.Register(nameof(NivelAudio), typeof(double),
            typeof(ReactorView), new PropertyMetadata(0.05));

    public static readonly DependencyProperty UsarSkiaProperty =
        DependencyProperty.Register(nameof(UsarSkia), typeof(bool),
            typeof(ReactorView), new PropertyMetadata(true, (_, _) => { }));

    public ReactorState Estado
    {
        get => (ReactorState)GetValue(EstadoProperty);
        set => SetValue(EstadoProperty, value);
    }

    public double NivelAudio
    {
        get => (double)GetValue(NivelAudioProperty);
        set => SetValue(NivelAudioProperty, value);
    }

    /// <summary>false = modo econômico (DrawingContext estático, sem Skia).</summary>
    public bool UsarSkia
    {
        get => (bool)GetValue(UsarSkiaProperty);
        set => SetValue(UsarSkiaProperty, value);
    }

    /// <summary>FPS medido do renderizador ativo (lido pela página Desempenho).</summary>
    public double FpsMedido { get; private set; }
    public string RenderizadorAtivo => UsarSkia && _skiaOk ? "SkiaSharp" : "WPF";

    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _relogio = Stopwatch.StartNew();
    private readonly DrawingHost _desenhoWpf;
    private int _quadros;
    private double _ultimoFps;
    private bool _skiaOk = true;
    private Window? _janela;
    private void JanelaMudou(object? sender, EventArgs e) => AtualizarTimer();

    public ReactorView()
    {
        InitializeComponent();
        _desenhoWpf = new DrawingHost(this);
        WpfHost.Content = _desenhoWpf;
        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / 30)
        };
        _timer.Tick += (_, _) => ProximoQuadro();
        IsVisibleChanged += (_, e) => AtualizarTimer();
        Loaded += (_, _) =>
        {
            _janela = Window.GetWindow(this);
            if (_janela is not null) _janela.StateChanged += JanelaMudou;
            AtualizarTimer();
        };
        Unloaded += (_, _) => { _timer.Stop(); if (_janela is not null) _janela.StateChanged -= JanelaMudou; _janela = null; };
    }

    private void AtualizarTimer()
    {
        // Para de renderizar com a janela minimizada, oculta ou coberta.
        var janela = Window.GetWindow(this);
        var visivel = IsVisible && janela is { WindowState: not WindowState.Minimized, Visibility: Visibility.Visible };
        if (visivel && !_timer.IsEnabled) _timer.Start();
        if (!visivel && _timer.IsEnabled) _timer.Stop();
    }

    private void ProximoQuadro()
    {
        _timer.Interval = TimeSpan.FromMilliseconds(ConfiguracaoApp.ModoEconomico || ConfiguracaoApp.ReduzirAnimacoes ? 1000 : 1000.0 / 24);
        var agora = _relogio.Elapsed.TotalSeconds;
        _quadros++;
        if (agora - _ultimoFps >= 1.0)
        {
            FpsMedido = _quadros / (agora - _ultimoFps);
            _quadros = 0;
            _ultimoFps = agora;
        }
        var economico = Estado == ReactorState.Economico || !UsarSkia;
        SkiaHost.Visibility = !economico && _skiaOk ? Visibility.Visible : Visibility.Collapsed;
        WpfHost.Visibility = economico || !_skiaOk ? Visibility.Visible : Visibility.Collapsed;
        try
        {
            if (!economico && _skiaOk) SkiaHost.InvalidateVisual();
            else _desenhoWpf.InvalidateVisual();
        }
        catch { _skiaOk = false; } // Fallback automático para Canvas 2D.
    }

    private void SkiaHost_PaintSurface(object? sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
    {
        try
        {
            SkiaReactorRenderer.Desenhar(e.Surface.Canvas, e.Info.Width, e.Info.Height,
                Estado, NivelAudio, _relogio.Elapsed.TotalSeconds);
        }
        catch { _skiaOk = false; }
    }

    private sealed class DrawingHost(ReactorView dono) : FrameworkElement
    {
        protected override void OnRender(DrawingContext dc)
        {
            // Sem Skia: versão estática achatada. Com Skia desligada mas WPF ativo: normal.
            var desenharComo = !dono.UsarSkia ? ReactorState.Economico : dono.Estado;
            WpfReactorRenderer.Desenhar(dc, ActualWidth, ActualHeight,
                desenharComo, dono.NivelAudio, dono._relogio.Elapsed.TotalSeconds);
        }
    }
}
