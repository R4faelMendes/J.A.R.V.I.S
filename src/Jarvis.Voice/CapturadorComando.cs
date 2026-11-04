namespace Jarvis.Voice;

/// <summary>Segmenta PCM 16 kHz mono, conservando o início da frase e as pausas naturais.</summary>
public sealed class CapturadorComando
{
    private const int BytesSegundo = 32000;
    private readonly Queue<byte[]> _antes = new();
    private readonly List<byte> _frase = new();
    private int _bytesAntes, _total, _silencio, _fala;
    private DateTime _inicio, _ultimoBloco, _ultimaFala;
    public bool Ativo { get; private set; }
    public void Iniciar()
    {
        _frase.Clear(); foreach (var b in _antes) _frase.AddRange(b);
        _total = _silencio = _fala = 0; Ativo = true;
        _inicio = _ultimoBloco = _ultimaFala = DateTime.UtcNow;
    }
    public void Cancelar() { Ativo = false; _frase.Clear(); }
    public void Limpar() { Cancelar(); _antes.Clear(); _bytesAntes = 0; }
    public byte[]? Alimentar(byte[] pcm, bool fala)
    {
        _antes.Enqueue(pcm); _bytesAntes += pcm.Length;
        while (_bytesAntes > BytesSegundo * 2 && _antes.Count > 1) _bytesAntes -= _antes.Dequeue().Length;
        if (!Ativo) return null;
        _ultimoBloco = DateTime.UtcNow;
        _frase.AddRange(pcm); _total += pcm.Length;
        if (fala) { _fala += pcm.Length; _silencio = 0; _ultimaFala = _ultimoBloco; } else _silencio += pcm.Length;
        // Janela inicial longa para “Jarvis” + pausa; fim por silêncio após a fala seguinte.
        var terminou = _fala >= BytesSegundo / 5 && _total >= BytesSegundo && _silencio >= BytesSegundo * 1.05;
        if (!terminou && _total < BytesSegundo * 10 && !(_fala == 0 && _total >= BytesSegundo * 3)) return null;
        Ativo = false; var audio = _frase.ToArray(); _frase.Clear(); return audio;
    }
    public byte[]? ExtrairPorInatividade(DateTime agora)
    {
        if (!Ativo || agora - _ultimoBloco < TimeSpan.FromMilliseconds(500)) return null;
        if (_fala > 0 ? agora - _ultimaFala < TimeSpan.FromSeconds(1.2) : agora - _inicio < TimeSpan.FromSeconds(3)) return null;
        Ativo = false; var audio = new byte[_frase.Count + BytesSegundo]; _frase.CopyTo(audio); _frase.Clear(); return audio;
    }
}
