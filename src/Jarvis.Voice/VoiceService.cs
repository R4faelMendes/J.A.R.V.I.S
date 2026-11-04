// JARVIS.Voice — captação WASAPI 16kHz mono + VAD + AGC + passa-alta + calibração.
using Jarvis.Core;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Jarvis.Voice;

/// <summary>Serviço de voz: implementa IVoiceService. O reconhecedor só roda com VAD ativo.</summary>
public sealed class VoiceService : IVoiceService, IDisposable
{
    private WasapiCapture? _captura;
    private MMDevice? _dispositivo;
    private readonly object _trava = new();
    private float _nivel;
    private bool _ligado = true;
    private double _limiarRuido = 0.02;
    private bool _falando;

    public event EventHandler<string>? TranscricaoParcial;
    public event EventHandler? OuvindoMudou;
    public event EventHandler? EscutaAtivada;

    /// <summary>Substring do microfone preferido (estável; índices mudam). Vazio = padrão do Windows.</summary>
    public string FiltroMicrofone { get; set; } = "";

    public string NomeMicrofone { get; private set; } = "";
    public string FormatoMicrofone { get; private set; } = "";
    public static IReadOnlyList<string> ListarMicrofones()
    {
        using var enumerador = new MMDeviceEnumerator();
        var nomes = new List<string>();
        foreach (var mic in enumerador.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)) { nomes.Add(mic.FriendlyName); mic.Dispose(); }
        return nomes;
    }
    public bool MicrofoneLigado
    {
        get { lock (_trava) return _ligado; }
    }

    public double NivelAudio
    {
        get { lock (_trava) return _nivel; }
    }

    /// <summary>Energia do último bloco considerado fala (para o reator).</summary>
    public bool FalandoVad
    {
        get { lock (_trava) return _falando; }
    }

    public void DefinirMicrofoneLigado(bool ligado)
    {
        lock (_trava) _ligado = ligado;
        OuvindoMudou?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Ativa a escuta (Ctrl+Espaço ou clique): a UI conecta o wake word aqui.</summary>
    public void AtivarEscuta() => EscutaAtivada?.Invoke(this, EventArgs.Empty);

    /// <summary>Resolve o microfone: nome preferido > padrão. Lista nomes se falhar.</summary>
    public static MMDevice ResolverMic(string? filtro = null)
    {
        using var disp = new MMDeviceEnumerator();
        var todos = disp.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            var achou = todos.FirstOrDefault(d =>
                d.FriendlyName.Contains(filtro, StringComparison.OrdinalIgnoreCase));
            if (achou is not null) return achou;
        }
        if (todos.Count == 0)
            throw new InvalidOperationException("Nenhum microfone ativo no Windows.");
        return disp.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
    }

    /// <summary>Calibra: mede o ruído de fundo por <paramref name="segundos"/> e define o limiar acima.</summary>
    public double Calibrar(int segundos = 2)
    {
        using var dev = ResolverMic(FiltroMicrofone);
        using var cap = new WasapiCapture(dev);
        cap.WaveFormat = new WaveFormat(16000, 16, 1);
        double soma = 0; int n = 0;
        cap.DataAvailable += (_, e) =>
        {
            for (var i = 0; i + 1 < e.BytesRecorded; i += 2)
            {
                var v = BitConverter.ToInt16(e.Buffer, i) / 32768.0;
                soma += v * v; n++;
            }
        };
        cap.StartRecording();
        Thread.Sleep(segundos * 1000);
        cap.StopRecording();
        var rms = n == 0 ? 0.02 : Math.Sqrt(soma / n);
        lock (_trava) _limiarRuido = Math.Max(0.008, rms * 3);
        return rms;
    }

    /// <summary>Inicia a captura contínua. <paramref name="aoBloco"/> recebe PCM 16kHz mono.</summary>
    public void Iniciar(Action<byte[]> aoBloco)
    {
        Parar();
        var dev = ResolverMic(FiltroMicrofone);
        _dispositivo = dev;
        var captura = new WasapiCapture(dev); // taxa nativa; reamostra abaixo
        _captura = captura;
        var conversor = new ConversorMicrofone(captura.WaveFormat);
        var preparador = new PreparadorAudio();
        NomeMicrofone = dev.FriendlyName;
        FormatoMicrofone = captura.WaveFormat.ToString();
        captura.DataAvailable += (_, e) =>
        {
            if (!MicrofoneLigado) return;
            var buf = conversor.Converter(e.Buffer, e.BytesRecorded);
            if (buf.Length == 0) return;
            buf = preparador.Processar(buf, ConfiguracaoApp.SensibilidadeMic);
            lock (_trava) { _nivel = (float)(_nivel * .6 + preparador.Nivel * .4); _falando = preparador.Fala; }
            aoBloco(buf);
        };        _captura.StartRecording();
    }

    public void Parar()
    {
        try { _captura?.StopRecording(); } catch { }
        _captura?.Dispose(); _captura = null;
        _dispositivo?.Dispose(); _dispositivo = null;
    }

    public void Dispose() => Parar();
}
