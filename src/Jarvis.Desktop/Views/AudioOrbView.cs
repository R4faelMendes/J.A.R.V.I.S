using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Jarvis.Core;
using Jarvis.Desktop.Renderers;
using SkiaSharp.Views.WPF;
namespace Jarvis.Desktop.Views;

/// <summary>Visualizador circular da voz do Jarvis, ocioso sem timer e com fallback WPF.</summary>
public sealed class AudioOrbView : Grid
{
    public static readonly DependencyProperty EstadoProperty = DependencyProperty.Register(nameof(Estado), typeof(ReactorState), typeof(AudioOrbView), new PropertyMetadata(ReactorState.Repouso, MudouPropriedade));
    public static readonly DependencyProperty NivelAudioProperty = DependencyProperty.Register(nameof(NivelAudio), typeof(double), typeof(AudioOrbView), new PropertyMetadata(0d, MudouPropriedade));
    public ReactorState Estado { get => (ReactorState)GetValue(EstadoProperty); set => SetValue(EstadoProperty, value); }
    public double NivelAudio { get => (double)GetValue(NivelAudioProperty); set => SetValue(NivelAudioProperty, value); }
    private readonly SKElement _canvas = new();
    private readonly Fallback _fallback = new() { Visibility = Visibility.Collapsed };
    private readonly DispatcherTimer _timer = new(DispatcherPriority.Render);
    private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();
    private Window? _window;
    private double _nivel, _ultimo;
    private bool _skia = true;
    private ReactorState EstadoVisual => _nivel > .004 && Estado is not (ReactorState.Falando or ReactorState.Ouvindo) ? ReactorState.Falando : Estado;
    private static void MudouPropriedade(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((AudioOrbView)d).Sincronizar();
    public AudioOrbView()
    {
        IsHitTestVisible = false; ClipToBounds = true; Children.Add(_canvas); Children.Add(_fallback);
        _canvas.PaintSurface += (_, e) => {
            try { AudioOrbRenderer.Desenhar(e.Surface.Canvas, e.Info.Width, e.Info.Height, _clock.Elapsed.TotalSeconds, _nivel, EstadoVisual); }
            catch { _skia = false; _canvas.Visibility = Visibility.Collapsed; _fallback.Visibility = Visibility.Visible; AtualizarDesenho(); }
        };
        _timer.Tick += (_, _) => {
            var agora = _clock.Elapsed.TotalSeconds;
            var alvo = Estado is ReactorState.Falando or ReactorState.Ouvindo ? NivelAudio : 0;
            _nivel = FormaEsferaAudio.Suavizar(_nivel, alvo, agora - _ultimo); _ultimo = agora;
            AtualizarDesenho();
            if (Estado is not (ReactorState.Falando or ReactorState.Ouvindo) && _nivel < .004) { _nivel = 0; _timer.Stop(); AtualizarDesenho(); }
        };
        Loaded += (_, _) => { _window = Window.GetWindow(this); if (_window is not null) _window.StateChanged += Mudou; ConfiguracaoApp.ConfiguracoesAlteradas += Mudou; TemasJarvis.Alterado += Mudou; Sincronizar(); };
        Unloaded += (_, _) => { _timer.Stop(); if (_window is not null) _window.StateChanged -= Mudou; ConfiguracaoApp.ConfiguracoesAlteradas -= Mudou; TemasJarvis.Alterado -= Mudou; _window = null; };
        IsVisibleChanged += (_, _) => Sincronizar();
    }
    private void Mudou(object? s, EventArgs e) => Dispatcher.InvokeAsync(Sincronizar);
    private void Sincronizar()
    {
        var ativo = IsLoaded && IsVisible && _window?.WindowState != WindowState.Minimized;
        var animar = Estado is ReactorState.Falando or ReactorState.Ouvindo || _nivel > .004;
        _timer.Interval = TimeSpan.FromMilliseconds(ConfiguracaoApp.ModoEconomico ? 125 : 1000d / 30);
        if (ativo && animar && !ConfiguracaoApp.ReduzirAnimacoes) {
            if (!_timer.IsEnabled) { _ultimo = _clock.Elapsed.TotalSeconds; _timer.Start(); }
        } else { _timer.Stop(); if (ConfiguracaoApp.ReduzirAnimacoes) _nivel = 0; }
        // Um nível recebido durante a fala é interpolado pelo timer; não redesenhe a cada binding.
        if (!_timer.IsEnabled) AtualizarDesenho();
    }
    private void AtualizarDesenho()
    {
        if (_skia) _canvas.InvalidateVisual();
        else { _fallback.Nivel = _nivel; _fallback.Tempo = _clock.Elapsed.TotalSeconds; _fallback.Estado = EstadoVisual; _fallback.InvalidateVisual(); }
    }
    private sealed class Fallback : FrameworkElement
    {
        public double Nivel, Tempo;
        public ReactorState Estado;
        protected override void OnRender(DrawingContext dc)
        {
            var center = new Point(ActualWidth / 2, ActualHeight / 2); var radius = Math.Min(ActualWidth, ActualHeight) * .285;
            var accent = (Color)ColorConverter.ConvertFromString(TemasJarvis.Atual.Acento);
            var fundo = (Color)ColorConverter.ConvertFromString(TemasJarvis.Atual.Fundo);
            var energia = Estado is ReactorState.Falando or ReactorState.Ouvindo ? Nivel : 0;
            var halo = new RadialGradientBrush(Color.FromArgb(38, accent.R, accent.G, accent.B), Colors.Transparent);
            dc.DrawEllipse(halo, null, center, radius * 1.7, radius * 1.7);
            var sphere = new RadialGradientBrush(Color.FromArgb(95, accent.R, accent.G, accent.B), fundo) { GradientOrigin = new Point(.3, .25) };
            dc.DrawEllipse(sphere, null, center, radius, radius);
            var geometry = new StreamGeometry(); using (var path = geometry.Open()) {
                for (var i = 0; i < 192; i++) { var a = i * Math.PI * 2 / 192; var r = radius * FormaEsferaAudio.Raio(a, Tempo, energia); var point = new Point(center.X + Math.Cos(a) * r, center.Y + Math.Sin(a) * r); if (i == 0) path.BeginFigure(point, false, true); else path.LineTo(point, true, false); }
            }
            dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb(40, accent.R, accent.G, accent.B)), 8), geometry);
            dc.DrawGeometry(null, new Pen(new SolidColorBrush(accent), 2), geometry);
        }
    }
}
