using System.Globalization;
using System.Windows;
using System.Windows.Media;
namespace Jarvis.Desktop.Views;
internal static class CorTelemetria
{
    public static Brush Aplicar(Brush brush) { if (brush is not SolidColorBrush solid) return brush; var c = solid.Color; var rgb = Jarvis.Core.TemasJarvis.Colorir((uint)((c.R << 16) | (c.G << 8) | c.B)); return new SolidColorBrush(Color.FromArgb(c.A, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb)); }
}
public sealed class TelemetryGauge : FrameworkElement
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(double), typeof(TelemetryGauge), new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.AffectsRender));
    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public Brush Accent { get; set; } = Brushes.DeepSkyBlue;
    protected override void OnRender(DrawingContext dc)
    {
        var text = new FormattedText(double.IsFinite(Value) ? $"{Value:0}%" : "—", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI Light"), 22, Brushes.AliceBlue, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(text, new Point(0, 0));
        var count = Math.Max(1, (int)(ActualWidth / 6));
        for (var i = 0; i < count; i++) {
            var filled = double.IsFinite(Value) && i < count * Math.Clamp(Value,0,100) / 100;
            dc.DrawRectangle(filled ? CorTelemetria.Aplicar(Accent) : CorTelemetria.Aplicar(new SolidColorBrush(Color.FromRgb(27,58,87))), null, new Rect(i * 6, Math.Max(30,ActualHeight - 6), 3, 4));
        }

    }
}
public sealed class TelemetryTrend : FrameworkElement
{
    private readonly Queue<double> _samples = new(); public Brush Accent { get; set; } = Brushes.DeepSkyBlue;
    public void Add(double? value) { if (value is null || !double.IsFinite(value.Value)) return; _samples.Enqueue(value.Value); while (_samples.Count > 24) _samples.Dequeue(); InvalidateVisual(); }
    protected override void OnRender(DrawingContext dc)
    {
        if (_samples.Count < 2) return; var values = _samples.ToArray(); var geometry = new StreamGeometry();
        using (var g = geometry.Open()) { for (var i = 0; i < values.Length; i++) { var p = new Point(i * ActualWidth / Math.Max(1, values.Length - 1), ActualHeight - Math.Clamp(values[i], 0, 100) / 100 * (ActualHeight - 4) - 2); if (i == 0) g.BeginFigure(p, false, false); else g.LineTo(p, true, false); } }
        dc.DrawGeometry(null, new Pen(CorTelemetria.Aplicar(Accent), 1.3), geometry);
    }
}
