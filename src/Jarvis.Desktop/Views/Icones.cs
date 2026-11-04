// JARVIS Desktop — ícones Lucide do protótipo (24px, traço 1.6) como Geometria XAML.
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Jarvis.Desktop.Views;

/// <summary>Converte os paths SVG do protótipo em Path WPF de 19px.</summary>
public static class Icones
{
    private static readonly Dictionary<string, string[]> Dados = new()
    {
        ["sparkles"] = new[]
        {
            "M11.017 2.814a1 1 0 0 1 1.966 0l1.051 5.558a2 2 0 0 0 1.594 1.594l5.558 1.051a1 1 0 0 1 0 1.966l-5.558 1.051a2 2 0 0 0-1.594 1.594l-1.051 5.558a1 1 0 0 1-1.966 0l-1.051-5.558a2 2 0 0 0-1.594-1.594l-5.558-1.051a1 1 0 0 1 0-1.966l5.558-1.051a2 2 0 0 0 1.594-1.594z M20 2v4 M22 4h-4",
        },
        ["message-square"] = new[]
        {
            "M22 17a2 2 0 0 1-2 2H6.828a2 2 0 0 0-1.414.586l-2.202 2.202A.71.71 0 0 1 2 21.286V5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2z",
        },
        ["layout-grid"] = new[]
        {
            "M3 3h7v7H3z M14 3h7v7h-7z M14 14h7v7h-7z M3 14h7v7H3z",
        },
        ["gauge"] = new[]
        {
            "M12 14l4-4 M3.34 19a10 10 0 1 1 17.32 0",
        },
        ["calendar-days"] = new[]
        {
            "M8 2v4 M16 2v4 M3 4h18v16H3z M3 10h18",
        },
        ["mail"] = new[]
        {
            "M22 7l-8.991 5.727a2 2 0 0 1-2.009 0L2 7 M2 4h20v16H2z",
        },
        ["newspaper"] = new[]
        {
            "M4 22h14a2 2 0 0 0 2-2V7l-5-5H6a2 2 0 0 0-2 2v18z M14 2v5h5 M8 10h8 M8 14h8 M8 18h8",
        },
        ["sun"] = new[]
        {
            "M12 8a4 4 0 1 0 0 8 4 4 0 0 0 0-8z M12 2v2 M12 20v2 M4.93 4.93l1.41 1.41 M17.66 17.66l1.41 1.41 M2 12h2 M20 12h2 M6.34 17.66l-1.41 1.41 M19.07 4.93l-1.41 1.41",
        },
        ["workflow"] = new[]
        {
            "M3 3h8v8H3z M7 11v4a2 2 0 0 0 2 2h4 M13 13h8v8h-8z",
        },
        ["smartphone"] = new[]
        {
            "M5 2h14v20H5z",
        },
        ["battery"] = new[] { "M2 7h17v10H2z M22 10v4 M6 10v4 M10 10v4 M14 10v4" },
        ["hard-drive"] = new[] { "M4 4h16l3 10v6H1v-6z M1 14h22 M6 17h1 M11 17h1" },
        ["wifi"] = new[] { "M2 8Q12 0 22 8 M5 12Q12 6 19 12 M8 16Q12 12 16 16 M12 20h.1" },
        ["monitor"] = new[] { "M2 3h20v14H2z M12 17v5 M7 22h10" },
        ["shield-check"] = new[]
        {
            "M20 13c0 5-3.5 7.5-7.66 8.95a1 1 0 0 1-.67-.01C7.5 20.5 4 18 4 13V6a1 1 0 0 1 1-1c2 0 4.5-1.2 6.24-2.72a1.17 1.17 0 0 1 1.52 0C14.51 3.81 17 5 19 5a1 1 0 0 1 1 1z M9 12l2 2 4-4",
        },
        ["settings"] = new[]
        {
            "M9.671 4.136a2.34 2.34 0 0 1 4.659 0 2.34 2.34 0 0 0 3.319 1.915 2.34 2.34 0 0 1 2.33 4.033 2.34 2.34 0 0 0 0 3.831 2.34 2.34 0 0 1-2.33 4.033 2.34 2.34 0 0 0-3.319 1.915 2.34 2.34 0 0 1-4.659 0 2.34 2.34 0 0 0-3.32-1.915 2.34 2.34 0 0 1-2.33-4.033 2.34 2.34 0 0 0 0-3.831A2.34 2.34 0 0 1 6.35 6.051a2.34 2.34 0 0 0 3.319-1.915",
        },
    };

    /// <summary>Grade 19px com o ícone centralizado, cor herdada do botão (currentColor).</summary>
    public static System.Windows.Controls.Grid Criar(string nome, double tamanho = 19, Brush? cor = null)
    {
        var grade = new System.Windows.Controls.Grid { Width = tamanho, Height = tamanho };
        System.Windows.Data.Binding CorDoBotao() => new System.Windows.Data.Binding("Foreground")
        {
            RelativeSource = new System.Windows.Data.RelativeSource(
                System.Windows.Data.RelativeSourceMode.FindAncestor, typeof(System.Windows.Controls.Button), 1),
        };
        if (!Dados.TryGetValue(nome, out var paths)) return grade;
        foreach (var d in paths)
        {
            var p = new Path
            {
                Data = Geometry.Parse(d),
                StrokeThickness = 1.6,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Fill = Brushes.Transparent,
                Stretch = Stretch.Uniform,
                Width = tamanho,
                Height = tamanho,
            };
            if (cor is null) p.SetBinding(Shape.StrokeProperty, CorDoBotao()); else p.Stroke = cor;
            grade.Children.Add(p);
        }
        // Pontos do calendário e círculos pequenos.
        void Ponto(double x, double y, double s, bool contorno)
        {
            Shape el = contorno
                ? new Ellipse { Width = s, Height = s, StrokeThickness = 1.2 }
                : new Ellipse { Width = s, Height = s };
            if (contorno) { if (cor is null) el.SetBinding(Shape.StrokeProperty, CorDoBotao()); else el.Stroke = cor; }
            else { if (cor is null) el.SetBinding(Shape.FillProperty, CorDoBotao()); else el.Fill = cor; }
            el.Margin = new System.Windows.Thickness(x * tamanho / 24 - s / 2, y * tamanho / 24 - s / 2, 0, 0);
            el.HorizontalAlignment = HorizontalAlignment.Left;
            el.VerticalAlignment = VerticalAlignment.Top;
            grade.Children.Add(el);
        }
        if (nome == "calendar-days")
            foreach (var (x, y) in new[] { (8, 14), (12, 14), (16, 14), (8, 18), (12, 18), (16, 18) })
                Ponto(x, y, 1.8, false);
        if (nome == "smartphone")
            Ponto(12, 18, 1.8, false);
        if (nome == "settings")
            Ponto(12, 12, 6, true);
        if (nome == "sparkles")
            Ponto(4, 20, 4, true);
        return grade;
    }
}

/// <summary>Estilos compartilhados de foco/acessibilidade.</summary>
public static class Estilos
{
    public static Style FocoCiano()
    {
        var estilo = new Style();
        estilo.Setters.Add(new Setter(Control.BorderBrushProperty,
            new SolidColorBrush(Color.FromRgb(0x4F, 0xD6, 0xFF))));
        estilo.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(2)));
        return estilo;
    }
}
