using System.IO;
using System.IO.Compression;
using SkiaSharp;
using Jarvis.Core;
namespace Jarvis.Rendering;

/// <summary>Quadros do estudo frontal v3, cache limitado e montagem por camadas.</summary>
public static class ReatorRealista
{
    private static readonly object Gate = new();
    private static readonly Lazy<ZipArchive?> Quadros = new(() => {
        var stream = typeof(ReatorRealista).Assembly.GetManifestResourceStream("Jarvis.Rendering.reactor-frontal-v3.zip");
        return stream is null ? null : new ZipArchive(stream, ZipArchiveMode.Read);
    });
    private static readonly Dictionary<int, SKBitmap> Cache = new();
    public static bool ArteDisponivel => Quadros.Value is not null;
    private static SKBitmap? Arte(int frame)
    {
        if (Cache.TryGetValue(frame, out var cached)) return cached;
        var entry = Quadros.Value?.GetEntry($"{frame:D4}.webp"); if (entry is null) return null;
        // O codec WebP precisa de leitura completa e acesso aleatório; a entrada
        // comprimida do ZIP pode retornar leituras parciais no .NET 8.
        using var stream = new MemoryStream();
        using (var origem = entry.Open()) origem.CopyTo(stream);
        stream.Position = 0;
        var image = SKBitmap.Decode(stream); if (image is null) return null;
        var pixels = image.Pixels; var half = image.Width * .5f;
        SKColor.Parse(TemasJarvis.Atual.Acento).ToHsv(out var hue, out _, out _);
        for (var y = 0; y < image.Height; y++) for (var x = 0; x < image.Width; x++) {
            var index = y * image.Width + x; var c = pixels[index];
            var radius = MathF.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / image.Width;
            if (radius > .435f) { pixels[index] = SKColors.Transparent; continue; }
            if (c.Blue <= c.Red + 4 || c.Green < c.Red || c.Blue < c.Green * .85) continue;
            var max = Math.Max(c.Red, Math.Max(c.Green, c.Blue)); var min = Math.Min(c.Red, Math.Min(c.Green, c.Blue)); float delta = max - min;
            if (max == 0 || delta < 7) continue;
            // Realça a energia verde sem alterar os metais e as áreas transparentes.
            {
                var brilho = Math.Min(255f, max * 1.12f);
                delta = Math.Min(brilho, delta * 1.4f);
                min = (byte)(brilho - delta);
            }
            var sector = (hue % 360) / 60f; var second = delta * (1 - Math.Abs(sector % 2 - 1));
            (float r, float g, float b) = sector switch { < 1 => (delta, second, 0f), < 2 => (second, delta, 0f), < 3 => (0f, delta, second), < 4 => (0f, second, delta), < 5 => (second, 0f, delta), _ => (delta, 0f, second) };
            pixels[index] = new SKColor((byte)(r + min), (byte)(g + min), (byte)(b + min), c.Alpha);
        }
        image.Pixels = pixels;
        if (Cache.Count >= 4) { var key = Cache.Keys.First(); Cache[key].Dispose(); Cache.Remove(key); }
        Cache[frame] = image; return image;
    }
    public static void Desenhar(SKCanvas canvas, int width, int height, double tempo, double nivel, double montagem = 1)
    {
        canvas.Clear(SKColors.Transparent); if (width <= 0 || height <= 0) return;
        lock (Gate) {
            var image = Arte((int)(Math.Max(0, tempo) * 25) % 400); if (image is null) return;
            var size = Math.Min(width, height) * .98f; var p = (float)Math.Clamp(montagem, 0, 1);
            using var paint = new SKPaint { IsAntialias = true };
            canvas.Save(); canvas.Translate(width * .5f, height * .5f);
            var accent = SKColor.Parse(TemasJarvis.Atual.Acento);
            using (var glow = SKShader.CreateRadialGradient(SKPoint.Empty, size * .5f, new[] { accent.WithAlpha((byte)(12 + 12 * p)), SKColors.Transparent }, null, SKShaderTileMode.Clamp)) {
                paint.Shader = glow; canvas.DrawCircle(0, 0, size * .5f, paint); paint.Shader = null;
            }
            canvas.Scale(1 + (float)Math.Clamp(nivel, 0, 1) * .006f);
            canvas.Scale(.65f + .35f * p);
            var target = new SKRect(-size / 2, -size / 2, size / 2, size / 2);
            if (p > .999f) canvas.DrawBitmap(image, target, paint);
            else {
                for (var ring = 0; ring < 3; ring++) {
                    var outer = size * (ring == 0 ? .435f : ring == 1 ? .305f : .235f);
                    var inner = size * (ring == 0 ? .305f : ring == 1 ? .235f : .14f);
                    var count = ring == 0 ? 10 : ring == 1 ? 10 : 3;
                    for (var i = 0; i < count; i++) {
                        var delay = ring * .075f + i / (float)count * .12f;
                        var local = Math.Clamp((p - delay) / (1 - delay), 0, 1);
                        // Aproximação em espiral, desaceleração e um pequeno encaixe mecânico.
                        var u = local - 1; var progress = 1 + 2.2f * u * u * u + 1.2f * u * u;
                        var spread = (1 - progress) * size * (.16f + ring * .03f);
                        var start = i * 360f / count - 90; var sweep = 360f / count;
                        var angle = (start + sweep / 2) * MathF.PI / 180;
                        var twist = (1 - local) * (ring % 2 == 0 ? 25 : -32);
                        using var clip = new SKPath();
                        clip.ArcTo(new SKRect(-outer, -outer, outer, outer), start, sweep, true);
                        clip.ArcTo(new SKRect(-inner, -inner, inner, inner), start + sweep, -sweep, false); clip.Close();
                        canvas.Save(); canvas.Translate(MathF.Cos(angle) * spread, MathF.Sin(angle) * spread);
                        var pivotX = MathF.Cos(angle) * (outer + inner) / 2; var pivotY = MathF.Sin(angle) * (outer + inner) / 2;
                        canvas.RotateDegrees(twist, pivotX, pivotY);
                        canvas.ClipPath(clip, SKClipOperation.Intersect, true); canvas.DrawBitmap(image, target, paint); canvas.Restore();
                        if (local > .15f && local < .85f) {
                            using var guide = new SKPaint { IsAntialias = true, Color = accent.WithAlpha((byte)(35 * MathF.Sin(local * MathF.PI))), StrokeWidth = .7f };
                            canvas.DrawLine(pivotX, pivotY, pivotX + MathF.Cos(angle) * spread, pivotY + MathF.Sin(angle) * spread, guide);
                        }
                    }
                }
                canvas.Save(); canvas.Scale(.86f + .14f * p);
                using var core = new SKPath(); core.AddCircle(0, 0, size * .14f);
                canvas.ClipPath(core, SKClipOperation.Intersect, true); canvas.DrawBitmap(image, target, paint); canvas.Restore();
            }
            if (p > .999f) {
                canvas.Save(); using var core = new SKPath(); core.AddCircle(0, 0, size * .116f);
                canvas.ClipPath(core, SKClipOperation.Intersect, true); canvas.RotateDegrees((float)(-tempo * 45 % 360));
                canvas.DrawBitmap(image, target, paint); canvas.Restore();
            }
            // Varredura e três satélites de energia: discreto em repouso, vivo ao falar.
            using var orbit = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = size * .0013f, Color = accent.WithAlpha((byte)(25 + 35 * p)) };
            canvas.DrawArc(new SKRect(-size * .455f, -size * .455f, size * .455f, size * .455f), (float)(tempo * 18 % 360), 24 + 20 * (float)nivel, false, orbit);
            for (var i = 0; i < 3; i++) {
                var angle = (float)(tempo * .22 + i * Math.PI * 2 / 3); orbit.Style = SKPaintStyle.Fill; orbit.Color = accent.WithAlpha((byte)(55 + 65 * p));
                canvas.DrawCircle(MathF.Cos(angle) * size * .455f, MathF.Sin(angle) * size * .455f, size * .0025f, orbit);
            }
            if (p > .72f && p < .999f) {
                orbit.Style = SKPaintStyle.Stroke; orbit.StrokeWidth = size * .003f; orbit.Color = accent.WithAlpha((byte)(90 * MathF.Sin((p - .72f) / .28f * MathF.PI)));
                canvas.DrawCircle(0, 0, size * (.14f + (p - .72f) * .9f), orbit);
            }
            canvas.Restore();
        }
    }
}
