// JARVIS Desktop — renderizador do reator via DrawingContext (WPF puro).
using System.Windows;
using System.Windows.Media;
using Jarvis.Core;

namespace Jarvis.Desktop.Renderers;

public static class WpfReactorRenderer
{
    private static Color CorEstado(ReactorState e) => e switch
    {
        ReactorState.Pensando => (Color)ColorConverter.ConvertFromString(Theme.Ouro),
        ReactorState.Alerta => (Color)ColorConverter.ConvertFromString(Theme.VermelhoNucleo),
        ReactorState.Mudo => Color.FromRgb(0x5A, 0x6B, 0x7D),
        _ => (Color)ColorConverter.ConvertFromString(TemasJarvis.Atual.Acento),
    };

    public static void Desenhar(DrawingContext dc, double w, double h, ReactorState estado, double nivel, double t)
    {
        var cx = w / 2; var cy = h / 2; var r = Math.Min(w, h) / 2 - 8;
        var cor = CorEstado(estado);
        var apagado = estado is ReactorState.Mudo or ReactorState.Economico || ConfiguracaoApp.ModoEconomico;
        var pulso = 1 + (estado == ReactorState.Falando ? nivel * 0.35 : 0.04 * Math.Sin(t * 1.5));

        var glow = new RadialGradientBrush(cor, Colors.Transparent);
        glow.GradientStops[0].Color = Color.FromArgb(apagado ? (byte)30 : (byte)100, cor.R, cor.G, cor.B);
        dc.DrawEllipse(glow, null, new Point(cx, cy), r * pulso, r * pulso);

        // Núcleo
        var nucleo = r * 0.34 * pulso;
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(apagado ? (byte)40 : (byte)140, cor.R, cor.G, cor.B)),
            null, new Point(cx, cy), nucleo, nucleo);
        dc.DrawEllipse(null, new Pen(new SolidColorBrush(cor), 2),
            new Point(cx, cy), nucleo * 1.25, nucleo * 1.25);
        var faseNucleo = ConfiguracaoApp.ReduzirAnimacoes ? 0 : -t * Math.PI / 15;
        for (var i = 0; i < 3; i++) {
            var a = faseNucleo + i * Math.PI * 2 / 3;
            dc.DrawEllipse(new SolidColorBrush(cor), null,
                new Point(cx + Math.Cos(a) * nucleo * 1.25, cy + Math.Sin(a) * nucleo * 1.25), 2.5, 2.5);
        }

        if (estado == ReactorState.Economico || ConfiguracaoApp.ModoEconomico)
        {
            dc.DrawEllipse(null, new Pen(new SolidColorBrush(cor), 1.5),
                new Point(cx, cy), r * 0.8, r * 0.34);
            return;
        }

        var vel = estado == ReactorState.Pensando ? 2.2 : estado == ReactorState.Ouvindo ? 0.9 : 0.25;
        for (var i = 0; i < 3; i++)
        {
            var rr = r * (0.55 + i * 0.15);
            var caneta = new Pen(new SolidColorBrush(Color.FromArgb(160, cor.R, cor.G, cor.B)), 1.5);
            dc.DrawEllipse(null, caneta, new Point(cx, cy), rr, rr * 0.38);
            var a = t * vel * (i % 2 == 0 ? 1 : -1) + i * 2.1;
            var px = cx + Math.Cos(a) * rr; var py = cy + Math.Sin(a) * rr * 0.38;
            dc.DrawEllipse(new SolidColorBrush(cor), null, new Point(px, py), 3, 3);
        }

        var canetaFina = new Pen(new SolidColorBrush(Color.FromArgb(90, cor.R, cor.G, cor.B)), 1);
        dc.DrawEllipse(null, canetaFina, new Point(cx, cy), r * 0.97, r * 0.97);
        for (var g = 0; g < 24; g++)
        {
            var a = g * Math.PI / 12;
            var x1 = cx + Math.Cos(a) * r * 0.97; var y1 = cy + Math.Sin(a) * r * 0.97;
            var x2 = cx + Math.Cos(a) * r * (g % 6 == 0 ? 0.88 : 0.93);
            var y2 = cy + Math.Sin(a) * r * (g % 6 == 0 ? 0.88 : 0.93);
            dc.DrawLine(canetaFina, new Point(x1, y1), new Point(x2, y2));
        }

        var n = estado == ReactorState.Pensando ? 70 : 40;
        for (var i = 0; i < n; i++)
        {
            var a = t * vel * 0.7 + i * 2.39996;
            var orb = r * (0.45 + (i * 37 % 50) / 100.0);
            var expand = estado == ReactorState.Ouvindo ? 1 + nivel * 0.4 : 1;
            var x = cx + Math.Cos(a) * orb * expand;
            var y = cy + Math.Sin(a) * orb * 0.5 * expand;
            var s = 1 + (i % 2);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(170, cor.R, cor.G, cor.B)), null,
                new Point(x, y), s, s);
        }

        if (estado == ReactorState.Alerta)
        {
            var dx = 3 * Math.Sin(t * 30);
            dc.DrawEllipse(null, new Pen(Brushes.Red, 2),
                new Point(cx + dx, cy), r * 0.7, r * 0.28);
        }
    }
}
