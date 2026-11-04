using Jarvis.Core;
using SkiaSharp;
namespace Jarvis.Tests;

public sealed class ReatorFrontalTest
{
    [Fact]
    public void QuadrosAnimadosMontagemETemaSaoRenderizados()
    {
        var anterior = TemasJarvis.Estado;
        try {
            Assert.True(Jarvis.Rendering.ReatorRealista.ArteDisponivel);
            var root = new DirectoryInfo(AppContext.BaseDirectory); while (root is not null && !File.Exists(Path.Combine(root.FullName, "Jarvis.sln"))) root = root.Parent;
            var output = Path.Combine(root!.FullName, "artifacts", "preview"); Directory.CreateDirectory(output);
            byte[] Render(string file, double time, double montagem) {
                using var surface = SKSurface.Create(new SKImageInfo(640, 640));
                Jarvis.Rendering.ReatorRealista.Desenhar(surface.Canvas, 640, 640, time, .05, montagem);
                using var image = surface.Snapshot(); using var png = image.Encode(SKEncodedImageFormat.Png, 100); var bytes = png.ToArray(); File.WriteAllBytes(Path.Combine(output, file + ".png"), bytes); return bytes;
            }
            TemasJarvis.Escolher("azul");
            var assembled = Render("Reator-frontal-montado", 0, 1);
            Assert.False(assembled.SequenceEqual(Render("Reator-frontal-girando", 2, 1)));
            Assert.False(assembled.SequenceEqual(Render("Reator-frontal-desmontado", 0, 0)));
            Render("Reator-frontal-encaixando", 1.5, .55);
            TemasJarvis.Escolher("ambar");
            var themed = Render("Reator-frontal-ambar", 0, 1);
            using var bitmap = SKBitmap.Decode(themed); var center = bitmap.GetPixel(320, 320);
            Assert.True(center.Red >= center.Blue, "O centro claro também deve deixar de ser ciano no tema âmbar.");
            Assert.Equal((byte)0, bitmap.GetPixel(0, 0).Alpha);
        } finally { TemasJarvis.Sincronizar(anterior); }
    }
}
