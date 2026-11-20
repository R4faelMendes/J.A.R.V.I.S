using Jarvis.Core;
using SkiaSharp;
namespace Jarvis.Desktop.Renderers;

public static class AudioOrbRenderer
{
    private static readonly (double Angulo, double Cosseno, double Seno)[] Contorno = PontosCirculo(256);
    private static readonly (double Angulo, double Cosseno, double Seno)[] Picos = PontosCirculo(96);
    private static readonly (float Largura, byte Opacidade)[] Tracos = [(.033f, 12), (.016f, 34), (.005f, 235)];
    private static readonly float[] ParadasHalo = [0f, .62f, 1f];
    private static readonly float[] ParadasEsfera = [0f, .64f, 1f];

    public static void Desenhar(SKCanvas canvas, int width, int height, double tempo, double nivel, ReactorState estado)
    {
        canvas.Clear(SKColors.Transparent); if (width <= 0 || height <= 0) return;
        var size = Math.Min(width, height); var radius = size * .285f;
        var energy = estado is ReactorState.Falando or ReactorState.Ouvindo ? (float)Math.Clamp(double.IsFinite(nivel) ? nivel : 0, 0, 1) : 0;
        var accent = SKColor.Parse(TemasJarvis.Atual.Acento); var background = SKColor.Parse(TemasJarvis.Atual.Fundo);
        canvas.Save(); canvas.Translate(width * .5f, height * .5f);
        using var paint = new SKPaint { IsAntialias = true };
        using (var halo = SKShader.CreateRadialGradient(SKPoint.Empty, radius * 1.7f, new[] { accent.WithAlpha((byte)(20 + 45 * energy)), accent.WithAlpha((byte)(8 + 16 * energy)), SKColors.Transparent }, ParadasHalo, SKShaderTileMode.Clamp)) {
            paint.Shader = halo; canvas.DrawCircle(0, 0, radius * 1.7f, paint); paint.Shader = null;
        }
        // Volume de vidro: luz deslocada para cima, sem peças mecânicas ou textura do reator.
        using (var sphere = SKShader.CreateRadialGradient(new SKPoint(-radius * .29f, -radius * .36f), radius * 1.6f,
            new[] { accent.WithAlpha((byte)(75 + 28 * energy)), background.WithAlpha(240), background.WithAlpha(220) }, ParadasEsfera, SKShaderTileMode.Clamp)) {
            paint.Shader = sphere; canvas.DrawCircle(0, 0, radius * (1 + energy * .025f), paint); paint.Shader = null;
        }
        using var contour = new SKPath();
        for (var i = 0; i < Contorno.Length; i++) {
            var (angle, cos, sin) = Contorno[i];
            var r = energy == 0 ? radius : radius * FormaEsferaAudio.Raio(angle, tempo, energy);
            var x = (float)(cos * r); var y = (float)(sin * r);
            if (i == 0) contour.MoveTo(x, y); else contour.LineTo(x, y);
        }
        contour.Close(); paint.Style = SKPaintStyle.Stroke; paint.StrokeJoin = SKStrokeJoin.Round;
        // Três traços translúcidos compõem o brilho sem filtros de desfoque por quadro.
        foreach (var (stroke, alpha) in Tracos) {
            paint.StrokeWidth = Math.Max(1, size * stroke); paint.Color = accent.WithAlpha(alpha); canvas.DrawPath(contour, paint);
        }
        paint.StrokeWidth = Math.Max(1, size * .0018f); paint.Color = accent.WithAlpha(48);
        canvas.DrawCircle(0, 0, radius * .91f, paint);
        if (energy > .008f) {
            // Pequenos picos radiais lembram o visualizador de graves da referência.
            paint.StrokeWidth = Math.Max(1, size * .0022f);
            foreach (var (angle, cos, sin) in Picos) {
                var wave = .5 + .5 * Math.Sin(angle * 13 + tempo * 5) * Math.Cos(angle * 7 - tempo * 3);
                var start = radius * FormaEsferaAudio.Raio(angle, tempo, energy) + size * .014;
                var end = start + size * energy * (.004 + .031 * wave);
                paint.Color = accent.WithAlpha((byte)(30 + 125 * energy * wave));
                canvas.DrawLine((float)(cos * start), (float)(sin * start), (float)(cos * end), (float)(sin * end), paint);
            }
            // Ecos de energia surgem e desaparecem com a voz, sem batida falsa em silêncio.
            paint.Color = accent.WithAlpha((byte)(32 * energy)); paint.StrokeWidth = Math.Max(1, size * .0015f);
            canvas.DrawCircle(0, 0, radius * (1.2f + energy * .17f), paint);
        }
        // Reflexo curvo reforça o volume esférico no interior do visualizador.
        paint.Color = SKColors.White.WithAlpha((byte)(22 + 16 * energy)); paint.StrokeWidth = size * .003f;
        canvas.DrawArc(new SKRect(-radius * .8f, -radius * .8f, radius * .8f, radius * .8f), 210, 48, false, paint);
        canvas.Restore();
    }

    private static (double Angulo, double Cosseno, double Seno)[] PontosCirculo(int quantidade)
    {
        var pontos = new (double, double, double)[quantidade];
        for (var i = 0; i < pontos.Length; i++)
        {
            var angulo = i * Math.PI * 2 / quantidade;
            pontos[i] = (angulo, Math.Cos(angulo), Math.Sin(angulo));
        }
        return pontos;
    }
}
