using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Jarvis.Core.Abstracoes;

namespace Jarvis.Desktop.Controles;

/// <summary>
/// Reator holográfico: núcleo luminoso + 3 anéis em perspectiva + reflexo.
/// Desenho leve com formas vetoriais (sem Viewport3D). Máximo 30 fps;
/// para quando a janela está oculta. Modo econômico vira estático.
/// </summary>
public sealed class VisaoReator : Canvas
{
    public static readonly DependencyProperty EstadoProperty =
        DependencyProperty.Register(nameof(Estado), typeof(EstadoReator), typeof(VisaoReator),
            new PropertyMetadata(EstadoReator.Repouso, (d, _) => ((VisaoReator)d).Redesenhar()));

    public static readonly DependencyProperty EconomicoProperty =
        DependencyProperty.Register(nameof(Economico), typeof(bool), typeof(VisaoReator),
            new PropertyMetadata(false, (d, _) => ((VisaoReator)d).Redesenhar()));

    public EstadoReator Estado
    {
        get => (EstadoReator)GetValue(EstadoProperty);
        set => SetValue(EstadoProperty, value);
    }

    public bool Economico
    {
        get => (bool)GetValue(EconomicoProperty);
        set => SetValue(EconomicoProperty, value);
    }

    private readonly Ellipse _nucleo = new();
    private readonly Ellipse[] _aneis = [new(), new(), new()];
    private readonly Ellipse _reflexo = new();
    private Storyboard? _animacao;

    public VisaoReator()
    {
        Width = 200; Height = 200;
        Children.Add(_reflexo);
        foreach (var anel in _aneis) Children.Add(anel);
        Children.Add(_nucleo);
        Loaded += (_, _) => Redesenhar();
        IsVisibleChanged += (_, e) =>
        {
            if (!(bool)e.NewValue) _animacao?.Pause();
            else _animacao?.Resume();
        };
    }

    private static Color CorDoEstado(EstadoReator estado) => estado switch
    {
        EstadoReator.Pensando => (Color)ColorConverter.ConvertFromString("#F4B544"),
        EstadoReator.Alerta => (Color)ColorConverter.ConvertFromString("#FF4D3D"),
        EstadoReator.Falando => (Color)ColorConverter.ConvertFromString("#4FD6FF"),
        EstadoReator.Mudo => (Color)ColorConverter.ConvertFromString("#6E8AA5"),
        _ => (Color)ColorConverter.ConvertFromString("#4FD6FF"),
    };

    private void Redesenhar()
    {
        _animacao?.Stop();
        Children.Clear();
        double lado = Math.Min(Width, Height);
        double cx = lado / 2, cy = lado / 2 - lado * 0.06;
        var cor = CorDoEstado(Estado);
        if (Estado == EstadoReator.Mudo) cor.A = 120;

        // Reflexo de piso em elipse.
        _reflexo.Width = lado * 0.62; _reflexo.Height = lado * 0.10;
        SetLeft(_reflexo, cx - _reflexo.Width / 2);
        SetTop(_reflexo, cy + lado * 0.30);
        _reflexo.Fill = new RadialGradientBrush(
            Color.FromArgb(60, cor.R, cor.G, cor.B),
            Colors.Transparent);
        Children.Add(_reflexo);

        // 3 anéis concêntricos inclinados (elipses achatadas).
        double[] raios = [0.30, 0.40, 0.50];
        for (int i = 0; i < 3; i++)
        {
            var anel = _aneis[i];
            anel.Width = lado * raios[i] * 2;
            anel.Height = lado * raios[i] * 2 * 0.42;
            SetLeft(anel, cx - anel.Width / 2);
            SetTop(anel, cy - anel.Height / 2);
            anel.Stroke = new SolidColorBrush(Color.FromArgb(
                (byte)(Estado == EstadoReator.Mudo ? 90 : 200 - i * 40),
                cor.R, cor.G, cor.B));
            anel.StrokeThickness = i == 0 ? 2.5 : 1.5;
            Children.Add(anel);
        }

        // Núcleo luminoso (gradiente pré-renderizado, sem blur por quadro).
        double rn = lado * (Estado == EstadoReator.Ouvindo ? 0.13 : 0.11);
        _nucleo.Width = rn * 2; _nucleo.Height = rn * 2;
        SetLeft(_nucleo, cx - rn);
        SetTop(_nucleo, cy - rn);
        _nucleo.Fill = new RadialGradientBrush(
            Colors.White,
            Color.FromArgb(255, cor.R, cor.G, cor.B))
        {
            GradientOrigin = new Point(0.5, 0.5),
            RadiusX = 0.5, RadiusY = 0.5
        };
        Children.Add(_nucleo);

        bool animar = !Economico && Estado != EstadoReator.Economico
            && !SystemParameters.HighContrast
            && Estado != EstadoReator.Mudo;
        if (!animar) return;

        // Respiração lenta do brilho (repouso) ou pulsos por estado.
        var pulso = new DoubleAnimation(0.75, 1.0,
            new Duration(TimeSpan.FromMilliseconds(Estado == EstadoReator.Repouso ? 2400 : 700)))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        _animacao = new Storyboard();
        _animacao.Children.Add(pulso);
        Storyboard.SetTarget(pulso, _nucleo);
        Storyboard.SetTargetProperty(pulso, new PropertyPath(OpacityProperty));
        _animacao.Begin(this, true);
    }
}
