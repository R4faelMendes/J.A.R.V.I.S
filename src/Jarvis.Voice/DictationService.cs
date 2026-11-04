// JARVIS.Voice — ditado livre com Whisper.net (tiny/base), sob demanda com descarga por ociosidade.
using Whisper.net;
using Whisper.net.Ggml;

namespace Jarvis.Voice;

/// <summary>Transcreve áudio 16kHz mono. Carrega o modelo ao usar e descarrega após 5 min parado.</summary>
public sealed class DictationService : IDisposable
{
    private readonly string _arquivoModelo;
    private readonly GgmlType _tipo;
    private WhisperFactory? _fabrica;
    private DateTime _ultimoUso = DateTime.MinValue;
    private readonly Timer _vigia;
    private readonly object _trava = new();
    private readonly SemaphoreSlim _processando = new(1, 1);
    private bool _emUso;

    public DictationService(string arquivoModelo, GgmlType tipo = GgmlType.Tiny)
    {
        _arquivoModelo = arquivoModelo;
        _tipo = tipo;
        _vigia = new Timer(_ => DescarregarSeOcioso(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public bool ModeloPresente => File.Exists(_arquivoModelo);
    public bool Carregado { get { lock (_trava) return _fabrica is not null; } }

    private void GarantirCarga()
    {
        lock (_trava)
        {
            if (_fabrica is null)
            {
                if (!ModeloPresente) throw new FileNotFoundException($"Modelo Whisper ausente: {_arquivoModelo}");
                _fabrica = WhisperFactory.FromPath(_arquivoModelo);
            }
            _ultimoUso = DateTime.UtcNow;
        }
    }

    private void DescarregarSeOcioso()
    {
        lock (_trava)
        {
            if (!_emUso && _fabrica is not null && DateTime.UtcNow - _ultimoUso > TimeSpan.FromMinutes(2))
            {
                _fabrica.Dispose();
                _fabrica = null;
            }
        }
    }

    /// <summary>Transcreve PCM 16kHz/16-bit/mono e devolve o texto.</summary>
    public async Task<string> TranscreverAsync(byte[] pcm16k, CancellationToken ct = default)
    {
        if (pcm16k.Length % 2 != 0) throw new ArgumentException("Áudio PCM16 incompleto.");
        if (pcm16k.Length < 6400) return "";
        await _processando.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            GarantirCarga(); WhisperFactory fabrica;
            lock (_trava) { _emUso = true; fabrica = _fabrica!; }
            var samples = new float[pcm16k.Length / 2]; double energia = 0;
            for (var i = 0; i < samples.Length; i++) { samples[i] = BitConverter.ToInt16(pcm16k, i * 2) / 32768f; energia += samples[i] * samples[i]; }
            if (Math.Sqrt(energia / samples.Length) < .001) return "";
            using var proc = fabrica.CreateBuilder().WithLanguage("pt").WithThreads(Math.Clamp(Environment.ProcessorCount / 2, 1, 4)).WithNoContext().WithPrompt("Jarvis. Português brasileiro. Aplicativos: calculadora, Spotify, Chrome, Edge, bloco de notas.").WithTemperature(0).Build();
            var sb = new System.Text.StringBuilder();
            await foreach (var seg in proc.ProcessAsync(samples, ct).ConfigureAwait(false)) sb.Append(seg.Text).Append(' ');
            return sb.ToString().Trim();
        }
        finally { lock (_trava) { _emUso = false; _ultimoUso = DateTime.UtcNow; } _processando.Release(); }
    }

    public void Dispose()
    {
        _vigia.Dispose();
        _processando.Wait();
        try { lock (_trava) { _fabrica?.Dispose(); _fabrica = null; } }
        finally { _processando.Release(); }
    }
}
