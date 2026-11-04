using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Jarvis.Core;
namespace Jarvis.Desktop.Views;
/// <summary>Arte tridimensional detalhada com profundidade por parallax, luz e reação à voz.</summary>
public sealed class ReactorCinematicView : Grid
{
    public static readonly DependencyProperty EstadoProperty = DependencyProperty.Register(nameof(Estado), typeof(ReactorState), typeof(ReactorCinematicView), new PropertyMetadata(ReactorState.Repouso));
    public static readonly DependencyProperty NivelAudioProperty = DependencyProperty.Register(nameof(NivelAudio), typeof(double), typeof(ReactorCinematicView), new PropertyMetadata(0.0));
    public ReactorState Estado { get => (ReactorState)GetValue(EstadoProperty); set => SetValue(EstadoProperty, value); }
    public double NivelAudio { get => (double)GetValue(NivelAudioProperty); set => SetValue(NivelAudioProperty, value); }
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(40) };
    private readonly ScaleTransform _scale = new(1, 1);
    private readonly TranslateTransform _offset = new();
    private readonly RotateTransform _tilt = new();
    private readonly System.Diagnostics.Stopwatch _time = System.Diagnostics.Stopwatch.StartNew();
    private Window? _window;
    private readonly ReactorEnergyView _energy = new();
    public ReactorCinematicView()
    {
        var halo = new System.Windows.Shapes.Ellipse { Width = 270, Height = 270, Fill = new RadialGradientBrush(Color.FromArgb(42, 30, 180, 245), Colors.Transparent), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        Children.Add(halo);
        var transform = new TransformGroup(); transform.Children.Add(_scale); transform.Children.Add(_tilt); transform.Children.Add(_offset);
        var layers = new Grid { RenderTransform = transform, RenderTransformOrigin = new Point(.5, .5), IsHitTestVisible = false };
        layers.Children.Add(new Image { Source = new BitmapImage(new Uri("pack://application:,,,/Jarvis.Desktop;component/Assets/Artwork/reactor-cinematic.png")), Stretch = Stretch.Uniform });
        layers.Children.Add(_energy); Children.Add(layers);
        _timer.Tick += (_, _) =>
        {
            var t = _time.Elapsed.TotalSeconds;
            var pulse = Estado is ReactorState.Ouvindo or ReactorState.Falando ? Math.Clamp(NivelAudio, 0, 1) * .045 : .002 * Math.Sin(t);
            _scale.ScaleX = _scale.ScaleY = 1 + pulse; _offset.Y = Math.Sin(t * .6) * 2.5;
            _tilt.Angle = Math.Sin(t * .35) * 1.5;
            _energy.Atualizar(t, NivelAudio);
        };
        Loaded += (_, _) => { _window = Window.GetWindow(this); if (_window is not null) _window.StateChanged += VisibilityChanged; ConfiguracaoApp.ConfiguracoesAlteradas += VisibilityChanged; Sync(); };
        Unloaded += (_, _) => { _timer.Stop(); if (_window is not null) _window.StateChanged -= VisibilityChanged; ConfiguracaoApp.ConfiguracoesAlteradas -= VisibilityChanged; };
        IsVisibleChanged += (_, _) => Sync();
        MouseMove += (_, e) => { if (ConfiguracaoApp.ReduzirAnimacoes || ConfiguracaoApp.ModoEconomico) return; _offset.X = (e.GetPosition(this).X / Math.Max(ActualWidth, 1) - .5) * 7; };
        MouseLeave += (_, _) => _offset.X = 0;
    }
    private void VisibilityChanged(object? sender, EventArgs e) => Dispatcher.InvokeAsync(Sync);
    private void Sync()
    {
        _timer.Interval = TimeSpan.FromMilliseconds(ConfiguracaoApp.ModoEconomico ? 125 : 33);
        if (IsLoaded && IsVisible && _window?.WindowState != WindowState.Minimized && !ConfiguracaoApp.ReduzirAnimacoes) _timer.Start();
        else { _timer.Stop(); _scale.ScaleX = _scale.ScaleY = 1; _tilt.Angle = 0; _offset.X = _offset.Y = 0; }
    }
}

internal sealed class ReactorEnergyView : FrameworkElement
{
    private double _tempo, _audio;
    public void Atualizar(double tempo, double audio) { _tempo = tempo; _audio = audio; InvalidateVisual(); }
    protected override void OnRender(DrawingContext dc)
    {
        var size = Math.Min(ActualWidth, ActualHeight); if (size <= 0) return;
        var x = (ActualWidth - size) / 2; var y = (ActualHeight - size) / 2;
        foreach (var trail in ReactorFluxo.Gerar(_tempo, _audio))
        {
            var path = new StreamGeometry();
            using (var g = path.Open())
            {
                var first = trail.Pontos[0]; g.BeginFigure(new Point(x + first.X * size, y + first.Y * size), false, false);
                g.PolyLineTo(trail.Pontos.Skip(1).Select(p => new Point(x + p.X * size, y + p.Y * size)).ToArray(), true, false);
            }
            path.Freeze();
            var color = trail.Ambar ? Color.FromRgb(255, 182, 84) : Color.FromRgb(93, 235, 255);
            color.A = (byte)(trail.Opacidade * 255);
            var brush = new SolidColorBrush(color); brush.Freeze();
            dc.DrawGeometry(null, new Pen(brush, Math.Max(.7, trail.Espessura * size)), path);
            var head = trail.Pontos[^1]; dc.DrawEllipse(brush, null, new Point(x + head.X * size, y + head.Y * size), size * .003, size * .003);
        }
    }
}

public sealed class VoiceWaveView : FrameworkElement
{
    public static readonly DependencyProperty NivelProperty = DependencyProperty.Register(nameof(Nivel), typeof(double), typeof(VoiceWaveView), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
    public double Nivel { get => (double)GetValue(NivelProperty); set => SetValue(NivelProperty, value); }
    protected override void OnRender(DrawingContext dc)
    {
        var pen = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString(TemasJarvis.Atual.Acento)), 2.4) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        for (var i = 0; i < 19; i++)
        {
            var h = 3 + (1 + Math.Sin(i * 1.7)) * (4 + Math.Clamp(Nivel, 0, 1) * 18);
            var x = 4 + i * Math.Max(3, (ActualWidth - 8) / 19); dc.DrawLine(pen, new Point(x, (ActualHeight - h) / 2), new Point(x, (ActualHeight + h) / 2));
        }
    }
}
