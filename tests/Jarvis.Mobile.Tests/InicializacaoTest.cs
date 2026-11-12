global using Microsoft.Maui.Controls;
global using Microsoft.Maui.Graphics;
global using Microsoft.Maui.ApplicationModel;
using Xunit;
namespace Jarvis.Mobile.Tests;
public class InicializacaoTest
{
    [Fact] public void ReatorMarkIvUsaArteRealEAnimaComSkiaDoMobile()
    {
        Assert.True(Jarvis.Rendering.ReatorRealista.ArteDisponivel);
        var previous = Jarvis.Core.TemasJarvis.Estado;
        try {
            byte[] Render(string tema, double t) {
                Jarvis.Core.TemasJarvis.Escolher(tema);
                using var surface = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(420,420));
                Jarvis.Rendering.ReatorRealista.Desenhar(surface.Canvas,420,420,t,.1);
                using var image = surface.Snapshot(); using var png = image.Encode(SkiaSharp.SKEncodedImageFormat.Png,100); return png.ToArray();
            }
            var first = Render("azul",0); var second = Render("azul",4); var amber = Render("ambar",4);
            Assert.False(first.SequenceEqual(second)); Assert.True(second.SequenceEqual(amber));
            using var decoded = SkiaSharp.SKBitmap.Decode(second); Assert.Equal(0, decoded.GetPixel(0,0).Alpha);
            var verdes = decoded.Pixels.Count(p => p.Alpha > 200 && p.Green > p.Red + 20 && p.Green > p.Blue + 20);
            Assert.True(verdes > 100, $"O reator deve conter energia verde visível; encontrados {verdes} pixels.");
        } finally { Jarvis.Core.TemasJarvis.Sincronizar(previous); }
    }
    [Fact] public void TemaAtualizaCartaoBotaoEGradienteSemRecriarAzul()
    {
        var before = Jarvis.Core.TemasJarvis.Estado;
        try {
            var button = new Button { BackgroundColor = Color.FromArgb("#4FD6FF"), TextColor = Colors.Black };
            var card = new Border { Content = button, Stroke = new SolidColorBrush(Color.FromArgb("#1B3A57")), Background = new LinearGradientBrush { GradientStops = { new GradientStop(Color.FromArgb("#102235"), 0), new GradientStop(Color.FromArgb("#08121E"), 1) } } };
            var root = new ContentPage { Content = card };
            foreach (var id in new[] { "rubi", "azul", "prata", "ambar", "violeta" }) {
                Jarvis.Core.TemasJarvis.Escolher(id);
                Services.TemaMobile.Aplicar(root);
                Assert.Equal(Color.FromArgb(Jarvis.Core.TemasJarvis.Atual.Acento), button.BackgroundColor);
                Assert.NotEqual(Color.FromArgb("#102235"), ((LinearGradientBrush)card.Background).GradientStops[0].Color);
                button.BackgroundColor = Color.FromArgb("#4FD6FF");
                Assert.Equal(Color.FromArgb(Jarvis.Core.TemasJarvis.Atual.Acento), button.BackgroundColor);
            }
        } finally { Jarvis.Core.TemasJarvis.Sincronizar(before); }
    }
    [Fact] public void ShellEControlesSemHandlerPodemReceberTema()
    {
        var shell = new Shell(); var page = new ContentPage { Content = new Image { Source = new FileImageSource() } };
        shell.Items.Add(new ShellContent { Content = page }); Services.TemaMobile.Aplicar(shell);
    }
    [Fact] public void TemaAceitaControlesNovosComBrushPadrao()
    {
        var root = new ContentPage(); var stack = new VerticalStackLayout(); root.Content = stack;
        stack.Add(new Border { Background = new SolidColorBrush() });
        stack.Add(new Label { Text = "Jarvis" }); stack.Add(new Entry()); stack.Add(new Border()); stack.Add(new Button { Text = "Conectar" }); stack.Add(new Image());
        Services.TemaMobile.Aplicar(root);
        Services.TemaMobile.Aplicar(root);
    }
}
