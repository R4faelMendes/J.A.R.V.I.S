using SkiaSharp;
namespace Jarvis.Mobile.Views;
public static class ReatorDetalhado
{
    public static void Desenhar(SKCanvas canvas, int width, int height, double tempo, double nivel) => Jarvis.Rendering.ReatorRealista.Desenhar(canvas, width, height, tempo, nivel);
}