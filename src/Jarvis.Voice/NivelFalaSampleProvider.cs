using NAudio.Wave;
namespace Jarvis.Voice;

/// <summary>Mede o PCM consumido pela saída de voz, sem alterar as amostras.</summary>
public sealed class NivelFalaSampleProvider(ISampleProvider fonte) : ISampleProvider
{
    private double _nivel;
    public WaveFormat WaveFormat => fonte.WaveFormat;
    public double Nivel => Volatile.Read(ref _nivel);
    public int Read(float[] buffer, int offset, int count)
    {
        var lidos = fonte.Read(buffer, offset, count);
        double energia = 0;
        for (var i = offset; i < offset + lidos; i++) {
            var sample = buffer[i]; if (float.IsFinite(sample)) energia += sample * (double)sample;
        }
        Volatile.Write(ref _nivel, lidos == 0 ? 0 : Math.Clamp(Math.Sqrt(energia / lidos) * 4, 0, 1));
        return lidos;
    }
}
