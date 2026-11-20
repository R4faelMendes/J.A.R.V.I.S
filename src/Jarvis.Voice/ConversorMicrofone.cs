using NAudio.Wave;
using NAudio.Wave.SampleProviders;
namespace Jarvis.Voice;

/// <summary>Conversão PCM/IEEE float com reamostragem filtrada e estado entre blocos.</summary>
public sealed class ConversorMicrofone
{
    private readonly BufferedWaveProvider _entrada;
    private readonly ISampleProvider _saida;
    private float[] _amostras = [];
    public ConversorMicrofone(WaveFormat formato)
    {
        if (formato is WaveFormatExtensible extensivel) formato = extensivel.ToStandardWaveFormat();
        _entrada = new BufferedWaveProvider(formato) { ReadFully = false, BufferDuration = TimeSpan.FromSeconds(5) };
        ISampleProvider mono = _entrada.ToSampleProvider();
        if (formato.Channels == 2) mono = new StereoToMonoSampleProvider(mono);
        else if (formato.Channels != 1) throw new NotSupportedException("Escolha um microfone mono ou estéreo no Windows.");
        _saida = formato.SampleRate == 16000 ? mono : new WdlResamplingSampleProvider(mono, 16000);
    }
    public byte[] Converter(byte[] dados, int quantidade)
    {
        _entrada.AddSamples(dados, 0, quantidade);
        var capacidade = (int)Math.Ceiling(quantidade / (double)_entrada.WaveFormat.BlockAlign * 16000 / _entrada.WaveFormat.SampleRate) + 64;
        if (_amostras.Length < capacidade) _amostras = new float[capacidade];
        var n = _saida.Read(_amostras, 0, capacidade);
        if (n == 0) return [];
        var pcm = new byte[n * 2];
        for (var i = 0; i < n; i++)
        {
            var v = float.IsFinite(_amostras[i]) ? Math.Clamp(_amostras[i], -1, 1) : 0;
            var s = (short)Math.Round(v * short.MaxValue);
            pcm[2 * i] = (byte)s; pcm[2 * i + 1] = (byte)(s >> 8);
        }
        return pcm;
    }
}
