namespace Jarvis.Voice;
/// <summary>Remove DC e aplica ganho suave limitado. Não elimina sílabas por um gate de volume.</summary>
public sealed class PreparadorAudio
{
    private double _anterior, _filtrado, _ganho = 1, _ruido = .0015;
    private int _amostrasRuido;
    private double _menorRuido = double.MaxValue;
    public bool Fala { get; private set; }
    public double Nivel { get; private set; }
    public byte[] Processar(byte[] pcm, double sensibilidade = .7)
    {
        if (pcm.Length == 0 || pcm.Length % 2 != 0) return [];
        var samples = new double[pcm.Length / 2]; double soma = 0;
        for (var i = 0; i < samples.Length; i++)
        {
            var x = BitConverter.ToInt16(pcm, i * 2) / 32768.0;
            var y = x - _anterior + .995 * _filtrado; _anterior = x; _filtrado = y; samples[i] = y; soma += y * y;
        }
        var rms = Math.Sqrt(soma / samples.Length);
        _amostrasRuido += samples.Length; _menorRuido = Math.Min(_menorRuido, rms);
        if (_amostrasRuido >= 32000)
        {
            if (_menorRuido < .015) _ruido = Math.Clamp(_menorRuido * 1.2, .0005, .008);
            _amostrasRuido = 0; _menorRuido = double.MaxValue;
        }
        if (rms < _ruido * 2) _ruido = Math.Clamp(_ruido * .98 + rms * .02, .0005, .008);
        var threshold = Math.Max(.002, _ruido * 2.3) * (1.5 - Math.Clamp(sensibilidade, .1, 1));
        Fala = rms >= threshold; Nivel = Math.Clamp(rms * 8, 0, 1);
        var desejado = rms > .001 ? Math.Clamp(.075 / rms, 1, 6) : 1;
        _ganho = _ganho * .85 + desejado * .15;
        var saida = new byte[pcm.Length];
        for (var i = 0; i < samples.Length; i++)
        {
            var valor = (short)Math.Round(Math.Clamp(samples[i] * _ganho, -.98, .98) * 32767);
            saida[2 * i] = (byte)valor; saida[2 * i + 1] = (byte)(valor >> 8);
        }
        return saida;
    }
}
