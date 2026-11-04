using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Jarvis.Core;
using SkiaSharp.Views.WPF;
namespace Jarvis.Desktop.Views;
public sealed class Reactor3DView : Grid
{
    public static readonly DependencyProperty EstadoProperty = DependencyProperty.Register(nameof(Estado), typeof(ReactorState), typeof(Reactor3DView), new PropertyMetadata(ReactorState.Repouso));
    public static readonly DependencyProperty NivelAudioProperty = DependencyProperty.Register(nameof(NivelAudio), typeof(double), typeof(Reactor3DView), new PropertyMetadata(0d));
    public ReactorState Estado { get => (ReactorState)GetValue(EstadoProperty); set => SetValue(EstadoProperty, value); }
    public double NivelAudio { get => (double)GetValue(NivelAudioProperty); set => SetValue(NivelAudioProperty, value); }
    private readonly SKElement _canvas = new();
    private readonly DispatcherTimer _timer = new(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(1000d / 30) };
    private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();
    private Window? _window;
    private double _montagem;
    private double _destino = 1;
    public void AlternarMontagem() { _destino = _destino == 1 ? 0 : 1; Sync(); }
    public Reactor3DView()
    {
        ClipToBounds = true; IsHitTestVisible = false; Children.Add(_canvas);
        var fallback = new ContentControl { Visibility = Visibility.Collapsed }; Children.Add(fallback);
        _canvas.PaintSurface += (_, e) => { try { Jarvis.Rendering.ReatorRealista.Desenhar(e.Surface.Canvas, e.Info.Width, e.Info.Height, ConfiguracaoApp.ReduzirAnimacoes || ConfiguracaoApp.ModoEconomico ? 0 : _clock.Elapsed.TotalSeconds, NivelAudio, ConfiguracaoApp.ReduzirAnimacoes || ConfiguracaoApp.ModoEconomico ? _destino : _montagem); } catch { _timer.Stop(); _canvas.Visibility = Visibility.Collapsed; fallback.Content = new ReactorView { UsarSkia = false }; fallback.Visibility = Visibility.Visible; } };
        _timer.Tick += (_, _) => { var passo = _timer.Interval.TotalSeconds / 3.0; _montagem = _destino > _montagem ? Math.Min(_destino, _montagem + passo) : Math.Max(_destino, _montagem - passo); _canvas.InvalidateVisual(); };
        Loaded += (_, _) => { _window = Window.GetWindow(this); if (_window is not null) _window.StateChanged += Mudou; ConfiguracaoApp.ConfiguracoesAlteradas += Mudou; TemasJarvis.Alterado += Mudou; Sync(); };
        Unloaded += (_, _) => { _timer.Stop(); if (_window is not null) _window.StateChanged -= Mudou; ConfiguracaoApp.ConfiguracoesAlteradas -= Mudou; TemasJarvis.Alterado -= Mudou; _window = null; };
        IsVisibleChanged += (_, _) => Sync();
    }
    private void Mudou(object? s, EventArgs e) => Dispatcher.InvokeAsync(Sync);
    private void Sync()
    {
        _timer.Interval = TimeSpan.FromMilliseconds(ConfiguracaoApp.ModoEconomico ? 125 : 1000d / 30);
        if (IsLoaded && IsVisible && _window?.WindowState != WindowState.Minimized && !ConfiguracaoApp.ReduzirAnimacoes && !ConfiguracaoApp.ModoEconomico) _timer.Start(); else _timer.Stop();
        _canvas.InvalidateVisual();
    }
}
