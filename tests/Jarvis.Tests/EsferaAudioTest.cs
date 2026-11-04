using Jarvis.Core;
using Jarvis.Voice;
using Jarvis.Desktop.Renderers;
using NAudio.Wave;
using SkiaSharp;
namespace Jarvis.Tests;

public sealed class EsferaAudioTest
{
    private sealed class Amostras(float[] values) : ISampleProvider {
        private int _position;
        public WaveFormat WaveFormat => WaveFormat.CreateIeeeFloatWaveFormat(22050, 1);
        public int Read(float[] buffer, int offset, int count) { var length = Math.Min(count, values.Length - _position); Array.Copy(values, _position, buffer, offset, length); _position += length; return length; }
    }
    [Fact] public void MedidorSegueOSomReproduzidoEPreservaAmostras()
    {
        var source = new float[] { .1f, -.1f, .2f, -.2f };
        var meter = new NivelFalaSampleProvider(new Amostras(source)); var buffer = new float[8];
        Assert.Equal(4, meter.Read(buffer, 2, 4)); Assert.Equal(source, buffer.Skip(2).Take(4));
        Assert.InRange(meter.Nivel, .63, .64);
        Assert.Equal(0, meter.Read(buffer, 0, 8)); Assert.Equal(0, meter.Nivel);
        var silence = new NivelFalaSampleProvider(new Amostras(new float[32])); silence.Read(new float[32], 0, 32); Assert.Equal(0, silence.Nivel);
    }
    [Fact] public void ContornoFechaEAtaqueEReleaseSaoLimitados()
    {
        Assert.Equal(FormaEsferaAudio.Raio(0, 2, .8), FormaEsferaAudio.Raio(Math.PI * 2, 2, .8), 9);
        Assert.Equal(1, FormaEsferaAudio.Raio(2, 0, 0)); Assert.Equal(1, FormaEsferaAudio.Raio(2, 80, 0));
        for (var i = 0; i < 256; i++) Assert.InRange(FormaEsferaAudio.Raio(i * Math.PI / 128, 9, 1), .94, 1.3);
        var attack = FormaEsferaAudio.Suavizar(0, 1, .04); var release = 1 - FormaEsferaAudio.Suavizar(1, 0, .04); Assert.True(attack > release);
        Assert.InRange(FormaEsferaAudio.Suavizar(double.NaN, double.NaN, 1), 0, 1);
    }
    [Fact] public void RenderizacaoReageAVozEFicaEstavelNoSilencio()
    {
        var previous = TemasJarvis.Estado;
        try {
            var root = new DirectoryInfo(AppContext.BaseDirectory); while (root is not null && !File.Exists(Path.Combine(root.FullName, "Jarvis.sln"))) root = root.Parent;
            var output = Path.Combine(root!.FullName, "artifacts", "preview"); Directory.CreateDirectory(output);
            byte[] Render(string nome, double time, double level, ReactorState state) {
                using var surface = SKSurface.Create(new SKImageInfo(640, 640)); AudioOrbRenderer.Desenhar(surface.Canvas, 640, 640, time, level, state);
                using var snapshot = surface.Snapshot(); using var data = snapshot.Encode(SKEncodedImageFormat.Png, 100); var bytes = data.ToArray();
                // Prévia composta sobre o fundo real da interface.
                using var preview = SKSurface.Create(new SKImageInfo(640, 640)); preview.Canvas.Clear(SKColor.Parse(TemasJarvis.Atual.Fundo)); preview.Canvas.DrawImage(snapshot, 0, 0);
                using var image = preview.Snapshot(); using var png = image.Encode(SKEncodedImageFormat.Png, 100); File.WriteAllBytes(Path.Combine(output, nome + ".png"), png.ToArray()); return bytes;
            }
            TemasJarvis.Escolher("azul");
            var silent = Render("Esfera-audio-repouso", 0, 0, ReactorState.Repouso);
            Assert.True(silent.SequenceEqual(Render("Esfera-audio-silencio", 8, .9, ReactorState.Repouso)));
            Assert.True(silent.SequenceEqual(Render("Esfera-audio-pausa-na-fala", 4, 0, ReactorState.Falando)));
            Assert.False(silent.SequenceEqual(Render("Esfera-audio-falando", 1.6, .8, ReactorState.Falando)));
            TemasJarvis.Escolher("ambar");
            Assert.False(silent.SequenceEqual(Render("Esfera-audio-ambar", 1.6, .8, ReactorState.Falando)));
        } finally { TemasJarvis.Sincronizar(previous); }
    }
}
