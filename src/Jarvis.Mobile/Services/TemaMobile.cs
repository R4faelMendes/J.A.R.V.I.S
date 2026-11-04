using System.Runtime.CompilerServices;
using Jarvis.Core;
namespace Jarvis.Mobile.Services;
public static class TemaMobile
{
    private sealed record CorAplicada(object Original, object Aplicado);
    private sealed class Estado { public bool Aplicando; public bool Observando; public Dictionary<BindableProperty, CorAplicada> Cores = new(); }
    private static readonly ConditionalWeakTable<BindableObject, Estado> Estados = new();
    public static void Iniciar()
    {
        TemasJarvis.Alterado += (_, _) => MainThread.BeginInvokeOnMainThread(() => { if (Application.Current?.MainPage is { } root) Aplicar(root); if (Shell.Current?.CurrentPage is { } page) Aplicar(page);
#if ANDROID
            JarvisWidget.AtualizarTodos();
#endif
        });
    }
    private static Color Map(Color c)
    {
        if (c is null) return Colors.Transparent;
        var rgb = TemasJarvis.Colorir((uint)((int)Math.Round(c.Red * 255) << 16 | (int)Math.Round(c.Green * 255) << 8 | (int)Math.Round(c.Blue * 255)));
        return Color.FromRgba((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, (byte)Math.Round(c.Alpha * 255));
    }
    private static Color Base(Color c) {
        var rgb = TemasJarvis.CorBase((uint)((int)Math.Round(c.Red * 255) << 16 | (int)Math.Round(c.Green * 255) << 8 | (int)Math.Round(c.Blue * 255)));
        return Color.FromRgba((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, (byte)Math.Round(c.Alpha * 255));
    }
    private static object GuardarBase(object value) => value switch {
        Color c => Base(c), SolidColorBrush { Color: { } c } => new SolidColorBrush(Base(c)),
        LinearGradientBrush l => new LinearGradientBrush(BaseStops(l.GradientStops), l.StartPoint, l.EndPoint),
        RadialGradientBrush r => new RadialGradientBrush(BaseStops(r.GradientStops), r.Center, r.Radius), _ => value };
    private static GradientStopCollection BaseStops(GradientStopCollection stops) { var result = new GradientStopCollection(); foreach (var stop in stops) result.Add(new GradientStop(Base(stop.Color), stop.Offset)); return result; }
    private static GradientStopCollection Stops(GradientStopCollection original) { var result = new GradientStopCollection(); foreach (var g in original) result.Add(new GradientStop(Map(g.Color), g.Offset)); return result; }
    private static Brush Map(Brush b) => b switch {
        SolidColorBrush { Color: { } color } => new SolidColorBrush(Map(color)),
        LinearGradientBrush l => new LinearGradientBrush(Stops(l.GradientStops), l.StartPoint, l.EndPoint),
        RadialGradientBrush r => new RadialGradientBrush(Stops(r.GradientStops), r.Center, r.Radius), _ => b };
    public static void Aplicar(Element element) => Aplicar(element, new HashSet<Element>(ReferenceEqualityComparer.Instance));
    private static void Aplicar(Element element, HashSet<Element> visitados)
    {
        if (!visitados.Add(element)) return;
        AplicarElemento(element);
        IEnumerable<Element> conteudo = element switch {
            ContentPage page when page.Content is { } content => [content],
            ContentView view when view.Content is { } content => [content],
            Border border when border.Content is { } content => [content],
            ScrollView scroll when scroll.Content is { } content => [content],
            Layout layout => layout.Children.OfType<Element>(),
            Shell shell => shell.Items.Cast<Element>(),
            ShellItem item => item.Items.Cast<Element>(),
            ShellSection section => section.Items.Cast<Element>(),
            ShellContent item when item.Content is Element content => [content], _ => [] };
        foreach (var child in conteudo.ToArray()) Aplicar(child, visitados);
        // A árvore lógica inclui conteúdo de cartões/páginas antes da criação dos handlers nativos.
        if (element is IElementController controller) foreach (var child in controller.LogicalChildren.ToArray()) Aplicar(child, visitados);
        if (element is Microsoft.Maui.IVisualTreeElement tree) foreach (var child in tree.GetVisualChildren().OfType<Element>().ToArray()) Aplicar(child, visitados);
    }
    private static void AplicarElemento(Element element)
    {
        var state = Estados.GetOrCreateValue(element); if (state.Aplicando) return; state.Aplicando = true;
        try
        {
            if (!state.Observando) { state.Observando = true; element.PropertyChanged += (_, e) => { if (!state.Aplicando && e.PropertyName is ("BackgroundColor" or "Background" or "TextColor" or "BorderColor" or "Stroke" or "Color" or "PlaceholderColor" or "TitleColor" or "OnColor" or "ThumbColor" or "ProgressColor" or "MinimumTrackColor" or "MaximumTrackColor" or "CancelButtonColor" or "Handler" or "Source")) AplicarElemento(element); }; }
            if (element is Shell shell) {
                Shell.SetBackgroundColor(shell, Color.FromArgb(TemasJarvis.Atual.Fundo)); Shell.SetTabBarBackgroundColor(shell, Color.FromArgb(TemasJarvis.Atual.Painel)); Shell.SetTabBarForegroundColor(shell, Color.FromArgb(TemasJarvis.Atual.Acento)); Shell.SetTabBarTitleColor(shell, Color.FromArgb(TemasJarvis.Atual.Acento)); Shell.SetForegroundColor(shell, Color.FromArgb(TemasJarvis.Atual.Acento));
            }
            var properties = new List<BindableProperty>();
            if (element is VisualElement) { properties.Add(VisualElement.BackgroundColorProperty); properties.Add(VisualElement.BackgroundProperty); }
            switch (element) {
                case Button: properties.AddRange([Button.TextColorProperty, Button.BorderColorProperty]); break;
                case Label: properties.Add(Label.TextColorProperty); break;
                case Entry: properties.AddRange([Entry.TextColorProperty, Entry.PlaceholderColorProperty]); break;
                case Editor: properties.AddRange([Editor.TextColorProperty, Editor.PlaceholderColorProperty]); break;
                case SearchBar: properties.AddRange([SearchBar.TextColorProperty, SearchBar.PlaceholderColorProperty, SearchBar.CancelButtonColorProperty]); break;
                case Picker: properties.AddRange([Picker.TextColorProperty, Picker.TitleColorProperty]); break;
                case DatePicker: properties.Add(DatePicker.TextColorProperty); break;
                case TimePicker: properties.Add(TimePicker.TextColorProperty); break;
                case Switch: properties.AddRange([Switch.OnColorProperty, Switch.ThumbColorProperty]); break;
                case CheckBox: properties.Add(CheckBox.ColorProperty); break;
                case ProgressBar: properties.Add(ProgressBar.ProgressColorProperty); break;
                case Slider: properties.AddRange([Slider.MinimumTrackColorProperty, Slider.MaximumTrackColorProperty, Slider.ThumbColorProperty]); break;
                case ActivityIndicator: properties.Add(ActivityIndicator.ColorProperty); break;
                case BoxView: properties.Add(BoxView.ColorProperty); break;
                case Border: properties.Add(Border.StrokeProperty); break;
            }
            foreach (var property in properties) {
                var current = element.GetValue(property); if (current is not (Color or Brush)) continue;
                if (!state.Cores.TryGetValue(property, out var saved) || !Equals(saved.Aplicado, current)) saved = new(GuardarBase(current), current);
                object mapped = saved.Original is Color c ? (object)Map(c) : Map((Brush)saved.Original);
                state.Cores[property] = new(saved.Original, mapped); if (!Equals(current, mapped)) element.SetValue(property, mapped);
            }
#if ANDROID
            if (element is Image { Source: FileImageSource source } image && source.File?.Contains("_icon.") == true && image.Handler?.PlatformView is Android.Widget.ImageView native) {
                var c = Color.FromArgb(TemasJarvis.Atual.Acento); native.SetColorFilter(new Android.Graphics.Color((byte)(c.Red * 255), (byte)(c.Green * 255), (byte)(c.Blue * 255)), Android.Graphics.PorterDuff.Mode.SrcIn!);
            }
#endif
        }
        finally { state.Aplicando = false; }
    }
}

