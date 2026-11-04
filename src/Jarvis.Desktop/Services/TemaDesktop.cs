using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Jarvis.Core;
namespace Jarvis.Desktop.Services;
public static class TemaDesktop
{
    private sealed record CorAplicada(Brush Original, Brush Aplicado);
    private static readonly ConditionalWeakTable<DependencyObject, Dictionary<DependencyProperty, CorAplicada>> Originais = new();
    private sealed class Observacao { public bool Aplicando; public bool Agendado; public Dictionary<DependencyProperty, (DependencyPropertyDescriptor Descriptor, EventHandler Handler)> Eventos = new(); }
    private static readonly ConditionalWeakTable<DependencyObject, Observacao> Observacoes = new();
    private static readonly Dictionary<object, object> RecursosOriginais = new();
    private static void AtualizarRecursos()
    {
        if (Application.Current is not { } app) return;
        foreach (var key in app.Resources.Keys.Cast<object>().ToArray()) {
            if (!RecursosOriginais.TryGetValue(key, out var original)) {
                var value = app.Resources[key];
                if (value is not (Brush or Color)) continue;
                RecursosOriginais[key] = original = value is Brush brush ? brush.CloneCurrentValue() : value;
            }
            app.Resources[key] = original is Color c ? Mapear(c) : Mapear((Brush)original);
        }
    }
    private static Color Mapear(Color c) { var rgb = TemasJarvis.Colorir((uint)((c.R << 16) | (c.G << 8) | c.B)); return Color.FromArgb(c.A, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb); }
    private static Brush Mapear(Brush original) {
        var brush = original.CloneCurrentValue();
        if (brush is SolidColorBrush solid) solid.Color = Mapear(solid.Color);
        if (brush is GradientBrush gradient) foreach (var stop in gradient.GradientStops) stop.Color = Mapear(stop.Color);
        if (brush.CanFreeze) brush.Freeze(); return brush;
    }
    private static Brush GuardarBase(Brush value)
    {
        var brush = value.CloneCurrentValue();
        static Color Base(Color c) { var rgb = TemasJarvis.CorBase((uint)((c.R << 16) | (c.G << 8) | c.B)); return Color.FromArgb(c.A, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb); }
        if (brush is SolidColorBrush solid) solid.Color = Base(solid.Color);
        if (brush is GradientBrush gradient) foreach (var stop in gradient.GradientStops) stop.Color = Base(stop.Color);
        return brush;
    }
    private static bool _iniciado;
    public static void Iniciar()
    {
        if (_iniciado) return; _iniciado = true; AtualizarRecursos();
        EventManager.RegisterClassHandler(typeof(FrameworkElement), FrameworkElement.LoadedEvent, new RoutedEventHandler((sender, _) => AplicarElemento((DependencyObject)sender)));
        EventManager.RegisterClassHandler(typeof(FrameworkElement), FrameworkElement.UnloadedEvent, new RoutedEventHandler((sender, _) => {
            if (!Observacoes.TryGetValue((DependencyObject)sender, out var state)) return;
            foreach (var entry in state.Eventos.Values) entry.Descriptor.RemoveValueChanged(sender, entry.Handler);
            state.Eventos.Clear();
        }));
        TemasJarvis.Alterado += (_, _) => {
            if (Application.Current is not { } app) return;
            void Atualizar() { AtualizarRecursos(); foreach (Window w in app.Windows) Aplicar(w); }
            if (app.Dispatcher.CheckAccess()) Atualizar(); else app.Dispatcher.InvokeAsync(Atualizar);
        };
    }
    public static void Aplicar(DependencyObject root)
    {
        AplicarElemento(root); for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) Aplicar(VisualTreeHelper.GetChild(root, i));
    }
    private static void AplicarElemento(DependencyObject element)
    {
        var properties = element switch {
            Border => new[] { Border.BackgroundProperty, Border.BorderBrushProperty, System.Windows.Documents.TextElement.ForegroundProperty },
            TextBlock => new[] { TextBlock.ForegroundProperty },
            System.Windows.Controls.Primitives.TextBoxBase => new[] { Control.BackgroundProperty, Control.ForegroundProperty, Control.BorderBrushProperty, System.Windows.Controls.Primitives.TextBoxBase.CaretBrushProperty, System.Windows.Controls.Primitives.TextBoxBase.SelectionBrushProperty },
            PasswordBox => new[] { Control.BackgroundProperty, Control.ForegroundProperty, Control.BorderBrushProperty, PasswordBox.CaretBrushProperty, PasswordBox.SelectionBrushProperty },
            Control => new[] { Control.BackgroundProperty, Control.ForegroundProperty, Control.BorderBrushProperty },
            Panel => new[] { Panel.BackgroundProperty, System.Windows.Documents.TextElement.ForegroundProperty },
            Shape => new[] { Shape.FillProperty, Shape.StrokeProperty }, FrameworkElement => new[] { System.Windows.Documents.TextElement.ForegroundProperty }, _ => Array.Empty<DependencyProperty>() };
        var state = Observacoes.GetOrCreateValue(element);
        if (state.Aplicando) return;
        state.Aplicando = true;
        try { foreach (var property in properties)
        {
            if (element is FrameworkElement { IsLoaded: true } && !state.Eventos.ContainsKey(property))
            {
                var descriptor = DependencyPropertyDescriptor.FromProperty(property, element.GetType());
                if (descriptor is not null) { EventHandler changed = (_, _) => {
                    if (state.Aplicando || state.Agendado) return;
                    state.Agendado = true;
                    element.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.DataBind, new Action(() => {
                        state.Agendado = false;
                        if (element is FrameworkElement { IsLoaded: true }) AplicarElemento(element);
                    }));
                }; descriptor.AddValueChanged(element, changed); state.Eventos[property] = (descriptor, changed); }
            }
            if (DependencyPropertyHelper.GetValueSource(element, property).BaseValueSource == BaseValueSource.Inherited) continue;
            if (element.GetValue(property) is not Brush value || value is not (SolidColorBrush or GradientBrush)) continue;
            // Mantenha os tokens vivos. Copiar um recurso já colorido para um
            // valor local fazia cartões e templates prenderem a paleta anterior.
            if (element is FrameworkElement tokenElement && Application.Current is { } application && !System.Windows.Data.BindingOperations.IsDataBound(element, property)) {
                var token = new[] { "FundoAbismo", "PainelVidroFundo", "PainelVidroEscuroFundo", "BordaLinha", "AcaoCiano", "TextoGelo", "TextoNevoa" }
                    .FirstOrDefault(key => ReferenceEquals(value, application.Resources[key]));
                if (token is not null && RecursosOriginais.TryGetValue(token, out var recursoOriginal) && recursoOriginal is Brush originalToken) {
                    var themed = Mapear(originalToken);
                    element.SetCurrentValue(property, themed);
                    Originais.GetOrCreateValue(element)[property] = new(originalToken, themed);
                    continue;
                }
                if (value is SolidColorBrush solid && solid.Color.A > 0) {
                    var c = solid.Color;
                    var blue = c.B > c.R * 1.18 && c.B >= c.G * .82 && c.B - c.R > 10;
                    var background = property == Border.BackgroundProperty || property == Control.BackgroundProperty || property == Panel.BackgroundProperty;
                    var border = property == Border.BorderBrushProperty || property == Control.BorderBrushProperty;
                    if (blue && c.A == 255 && (background && c.B < 110 || border)) {
                        var key = border ? "BordaLinha" : c.B <= 26 ? "FundoAbismo" : "PainelVidroEscuroFundo";
                        var originalTokenBrush = (Brush)RecursosOriginais[key]; var themed = Mapear(originalTokenBrush);
                        element.SetCurrentValue(property, themed); Originais.GetOrCreateValue(element)[property] = new(originalTokenBrush, themed); continue;
                    }
                }
            }
            var originals = Originais.GetOrCreateValue(element); if (!originals.TryGetValue(property, out var saved) || !ReferenceEquals(saved.Aplicado, value)) saved = new(GuardarBase(value), value);
            var original = saved.Original;
            var brush = Mapear(original); element.SetCurrentValue(property, brush); originals[property] = new(original, brush);
        }
        } finally { state.Aplicando = false; }
        if (element is FrameworkElement visual) visual.InvalidateVisual();
    }
}
