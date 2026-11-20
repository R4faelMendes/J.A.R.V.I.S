using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Jarvis.Core;

namespace Jarvis.Desktop.Services;

public static class TemaDesktop
{
    private sealed class Observacao
    {
        public bool Aplicando;
        public bool Agendado;
        public Dictionary<DependencyProperty, DependencyPropertyDescriptor> Propriedades { get; } = new();
    }

    private static readonly ConditionalWeakTable<FrameworkElement, Observacao> Observacoes = new();
    private static readonly EventHandler PincelAlterado = AoAlterarPincel;
    private static readonly DependencyProperty[] PinceisBorda = [Border.BackgroundProperty, Border.BorderBrushProperty, TextElement.ForegroundProperty];
    private static readonly DependencyProperty[] PinceisTexto = [TextBlock.ForegroundProperty];
    private static readonly DependencyProperty[] PinceisCampo = [Control.BackgroundProperty, Control.ForegroundProperty, Control.BorderBrushProperty, TextBoxBase.CaretBrushProperty, TextBoxBase.SelectionBrushProperty];
    private static readonly DependencyProperty[] PinceisSenha = [Control.BackgroundProperty, Control.ForegroundProperty, Control.BorderBrushProperty, PasswordBox.CaretBrushProperty, PasswordBox.SelectionBrushProperty];
    private static readonly DependencyProperty[] PinceisControle = [Control.BackgroundProperty, Control.ForegroundProperty, Control.BorderBrushProperty];
    private static readonly DependencyProperty[] PinceisPainel = [Panel.BackgroundProperty, TextElement.ForegroundProperty];
    private static readonly DependencyProperty[] PinceisForma = [Shape.FillProperty, Shape.StrokeProperty];
    private static readonly DependencyProperty[] PinceisElemento = [TextElement.ForegroundProperty];
    private static bool _iniciado;

    public static void Iniciar()
    {
        if (_iniciado) return;

        _iniciado = true;
        AtualizarRecursos();
        EventManager.RegisterClassHandler(typeof(FrameworkElement), FrameworkElement.LoadedEvent,
            new RoutedEventHandler((sender, _) =>
            {
                if (sender is FrameworkElement elemento) AplicarElemento(elemento);
            }));
        EventManager.RegisterClassHandler(typeof(FrameworkElement), FrameworkElement.UnloadedEvent,
            new RoutedEventHandler((sender, _) =>
            {
                if (sender is not FrameworkElement elemento || !Observacoes.TryGetValue(elemento, out var estado)) return;
                foreach (var descriptor in estado.Propriedades.Values) descriptor.RemoveValueChanged(elemento, PincelAlterado);
                estado.Propriedades.Clear();
            }));
    }

    public static void Aplicar(DependencyObject raiz)
    {
        if (raiz is FrameworkElement elemento) AplicarElemento(elemento);
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(raiz); i++)
            Aplicar(VisualTreeHelper.GetChild(raiz, i));
    }

    private static void AtualizarRecursos()
    {
        if (Application.Current is not { } app) return;
        foreach (var key in app.Resources.Keys.Cast<object>().ToArray())
        {
            switch (app.Resources[key])
            {
                case Brush pincel:
                    var novo = Mapear(pincel);
                    if (!ReferenceEquals(pincel, novo)) app.Resources[key] = novo;
                    break;
                case Color cor:
                    var mapeada = Mapear(cor);
                    if (mapeada != cor) app.Resources[key] = mapeada;
                    break;
            }
        }
    }

    private static Color Mapear(Color cor)
    {
        var rgb = TemasJarvis.Colorir((uint)((cor.R << 16) | (cor.G << 8) | cor.B));
        return Color.FromArgb(cor.A, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
    }

    private static Brush Mapear(Brush pincel)
    {
        if (pincel is SolidColorBrush solido)
        {
            var cor = Mapear(solido.Color);
            if (cor == solido.Color) return pincel;

            var novo = solido.CloneCurrentValue();
            novo.Color = cor;
            return Congelar(novo);
        }

        if (pincel is GradientBrush gradiente)
        {
            foreach (var ponto in gradiente.GradientStops)
            {
                if (Mapear(ponto.Color) == ponto.Color) continue;

                var novo = gradiente.CloneCurrentValue();
                foreach (var item in novo.GradientStops) item.Color = Mapear(item.Color);
                return Congelar(novo);
            }
        }

        return pincel;
    }

    private static Brush Congelar(Brush pincel)
    {
        if (pincel.CanFreeze) pincel.Freeze();
        return pincel;
    }

    private static void AoAlterarPincel(object? sender, EventArgs e)
    {
        if (sender is not FrameworkElement elemento || !Observacoes.TryGetValue(elemento, out var estado) ||
            estado.Aplicando || estado.Agendado) return;

        estado.Agendado = true;
        elemento.Dispatcher.BeginInvoke(DispatcherPriority.DataBind, new Action(() =>
        {
            estado.Agendado = false;
            if (elemento.IsLoaded) AplicarElemento(elemento);
        }));
    }

    private static void AplicarElemento(FrameworkElement elemento)
    {
        var propriedades = elemento switch
        {
            Border => PinceisBorda,
            TextBlock => PinceisTexto,
            TextBoxBase => PinceisCampo,
            PasswordBox => PinceisSenha,
            Control => PinceisControle,
            Panel => PinceisPainel,
            Shape => PinceisForma,
            _ => PinceisElemento
        };
        var estado = Observacoes.GetOrCreateValue(elemento);
        if (estado.Aplicando) return;

        estado.Aplicando = true;
        var alterou = false;
        try
        {
            foreach (var propriedade in propriedades)
            {
                if (elemento.IsLoaded && !estado.Propriedades.ContainsKey(propriedade))
                {
                    var descriptor = DependencyPropertyDescriptor.FromProperty(propriedade, elemento.GetType());
                    if (descriptor is not null)
                    {
                        descriptor.AddValueChanged(elemento, PincelAlterado);
                        estado.Propriedades[propriedade] = descriptor;
                    }
                }

                if (DependencyPropertyHelper.GetValueSource(elemento, propriedade).BaseValueSource == BaseValueSource.Inherited)
                    continue;
                if (elemento.GetValue(propriedade) is not Brush pincel) continue;

                var token = TokenDeFundoOuBorda(elemento, propriedade, pincel);
                var mapeado = Mapear(token ?? pincel);
                if (ReferenceEquals(pincel, mapeado)) continue;

                elemento.SetCurrentValue(propriedade, mapeado);
                alterou = true;
            }
        }
        finally { estado.Aplicando = false; }

        if (alterou) elemento.InvalidateVisual();
    }

    private static Brush? TokenDeFundoOuBorda(FrameworkElement elemento, DependencyProperty propriedade, Brush pincel)
    {
        if (pincel is not SolidColorBrush solido) return null;

        var cor = solido.Color;
        if (cor.A != 255 || cor.B <= cor.R * 1.18 || cor.B < cor.G * .82 || cor.B - cor.R <= 10) return null;

        var borda = propriedade == Border.BorderBrushProperty || propriedade == Control.BorderBrushProperty;
        var fundo = propriedade == Border.BackgroundProperty || propriedade == Control.BackgroundProperty || propriedade == Panel.BackgroundProperty;
        if ((!borda && (!fundo || cor.B >= 110)) || BindingOperations.IsDataBound(elemento, propriedade)) return null;

        var chave = borda ? "BordaLinha" : cor.B <= 26 ? "FundoAbismo" : "PainelVidroEscuroFundo";
        return Application.Current?.Resources[chave] as Brush;
    }
}
