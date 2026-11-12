using Jarvis.Core;
using Vosk;
namespace Jarvis.Voice;
public sealed record SegmentoVoz(byte[] Audio, string TextoVosk, bool VerificarAtivacao);

/// <summary>Transcrições finais para comandos, parciais apenas para feedback.</summary>
public sealed class WakeWordListener : IDisposable
{
    private readonly VoiceService _voz;
    private readonly object _gate = new();
    private Model? _modelo;
    private VoskRecognizer? _rec;
    private volatile bool _rodando;
    private volatile bool _suspenso;
    private string _ultimaParcial = "";
    private readonly CapturadorComando _captura = new();
    private readonly CapturadorComando _verificacao = new();
    private bool _processando;
    private string _fraseVosk = "";
    private readonly Timer _vigia;
    public event EventHandler<SegmentoVoz>? SegmentoPronto;
    public event EventHandler<byte[]>? AudioComandoPronto;
    public event EventHandler<byte[]>? AudioParaVerificarAtivacao;
    public event EventHandler? AoOuvirJarvis;
    public event EventHandler<string>? AoTranscrever;
    public event EventHandler<string>? Parcial;
    public event EventHandler<string>? ComandoDetectado;
    public WakeWordListener(VoiceService voz) { _voz = voz; _vigia = new Timer(_ => VerificarPausa(), null, 200, 200); }
    private void VerificarPausa()
    {
        SegmentoVoz? segmento = null;
        lock (_gate)
        {
            if (!_rodando || _suspenso || _processando) return;
            var audio = _captura.ExtrairPorInatividade(DateTime.UtcNow); var verificar = false;
            if (audio is null) { audio = _verificacao.ExtrairPorInatividade(DateTime.UtcNow); verificar = true; }
            if (audio is not null) { _processando = true; segmento = new(audio, _fraseVosk, verificar); }
        }
        if (segmento is not null) SegmentoPronto?.Invoke(this, segmento);
    }
    public void Carregar(string pastaModelo)
    {
        // Vosk pode encerrar o processo em código nativo quando a pasta não é um modelo.
        // Distribuições portáteis começam sem modelos: falhe antes de entrar na DLL.
        if (!Directory.Exists(pastaModelo))
            throw new DirectoryNotFoundException("Instale o modelo Vosk em: " + pastaModelo);
        if (!ModeloVosk.Completo(pastaModelo)) throw new FileNotFoundException("Modelo Vosk incompleto. Reinstale o modelo de voz.", pastaModelo);
        lock (_gate)
        {
            _rec?.Dispose(); _modelo?.Dispose();
            _modelo = new Model(pastaModelo); _rec = new VoskRecognizer(_modelo, 16000f);
        }
    }
    public void Suspender(bool suspenso)
    {
        lock (_gate) { _suspenso = suspenso; _captura.Limpar(); _verificacao.Limpar(); _rec?.Reset(); _ultimaParcial = ""; }
    }
    public void AtivarComando()
    {
        lock (_gate) { _processando = false; _verificacao.Cancelar(); _captura.Iniciar(); }
        AoOuvirJarvis?.Invoke(this, EventArgs.Empty);
    }
    public void LiberarComando(bool preservarAudio = false) { lock (_gate) { _processando = false; if (preservarAudio) { _captura.Cancelar(); _verificacao.Cancelar(); } else { _captura.Limpar(); _verificacao.Limpar(); } _rec?.Reset(); _ultimaParcial = _fraseVosk = ""; } }
    public void Iniciar()
    {
        if (_rec is null) throw new InvalidOperationException("Carregue o reconhecedor primeiro.");
        _rodando = true;
        _voz.Iniciar(bloco => { if (_rodando) ProcessarPcm(bloco, _voz.FalandoVad); });
    }
    /// <summary>Entrada PCM16 mono/16 kHz compartilhada pela captura e reprodução de diagnóstico.</summary>
    public void ProcessarPcm(byte[] bloco, bool fala)
    {
            string texto, frase; bool final; byte[]? audio = null, verificar = null;
            lock (_gate)
            {
                if (_rec is null) return;
                if (!_suspenso) audio = _captura.Alimentar(bloco, fala);
                if (!_suspenso && !_captura.Ativo && !_processando && audio is null && ConfiguracaoApp.WakeWordAtiva)
                {
                    if (fala && !_verificacao.Ativo) _verificacao.Iniciar();
                    verificar = _verificacao.Alimentar(bloco, fala);
                }
                if (audio is not null) _processando = true;
                final = _rec.AcceptWaveform(bloco, bloco.Length);
                texto = Extrair(final ? _rec.Result() : _rec.PartialResult(), final ? "text" : "partial");
                if (!string.IsNullOrWhiteSpace(texto)) _fraseVosk = texto;
                frase = _fraseVosk;
                if (audio is not null || verificar is not null) _processando = true;
                if (audio is null && verificar is null && !final && texto == _ultimaParcial) return;
                _ultimaParcial = final ? "" : texto;
            }
            if (audio is not null) { SegmentoPronto?.Invoke(this, new(audio, frase, false)); AudioComandoPronto?.Invoke(this, audio); return; }
            if (verificar is not null)
            {
                SegmentoPronto?.Invoke(this, new(verificar, frase, true));
                AudioParaVerificarAtivacao?.Invoke(this, verificar); return;
            }
            if (string.IsNullOrWhiteSpace(texto)) return;
            if (_suspenso)
            {
                // Durante TTS, reconhecer somente a interrupção explícita; nenhum outro comando executa.
                if (final && ConfiguracaoApp.WakeWordAtiva && WakeWordMatcher.ContainsWakeWord(texto, ConfiguracaoApp.PalavraAtivacao))
                {
                    var stop = WakeWordMatcher.StripWakeWord(texto, ConfiguracaoApp.PalavraAtivacao).Trim();
                    if (stop is "pare" or "parar" or "cancelar") ComandoDetectado?.Invoke(this, stop);
                }
                return;
            }
            if (!final) { bool ouvindo; lock (_gate) ouvindo = _captura.Ativo; if (ouvindo) Parcial?.Invoke(this, texto); }
            else AoTranscrever?.Invoke(this, texto);
            bool ativar;
            lock (_gate) ativar = !_processando && !_captura.Ativo;
            if (ativar && ConfiguracaoApp.WakeWordAtiva && WakeWordMatcher.AtivacaoNoInicio(texto, ConfiguracaoApp.PalavraAtivacao))
            {
                AtivarComando();
            }

    }
    private static string Extrair(string json, string campo)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty(campo, out var v) ? v.GetString() ?? "" : "";
    }
    public void Parar() { _rodando = false; _voz.Parar(); }
    public void Dispose() { _vigia.Dispose(); Parar(); lock (_gate) { _rec?.Dispose(); _rec = null; _modelo?.Dispose(); _modelo = null; } }
}
