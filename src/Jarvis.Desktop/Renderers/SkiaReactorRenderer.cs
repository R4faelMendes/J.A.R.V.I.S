using Jarvis.Core;
using SkiaSharp;
namespace Jarvis.Desktop.Renderers;
public static class SkiaReactorRenderer
{
    public static void Desenhar(SKCanvas canvas, int width, int height, ReactorState estado, double nivel, double tempo) => Jarvis.Rendering.ReatorRealista.Desenhar(canvas, width, height, ConfiguracaoApp.ReduzirAnimacoes ? 0 : tempo, nivel);
}