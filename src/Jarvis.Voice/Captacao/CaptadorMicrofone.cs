using NAudio.CoreAudioApi;
using NAudio.Wave;
using Jarvis.Core.Abstracoes;

namespace Jarvis.Voice.Captacao;

/// <summary>
/// Captação WASAPI 16 kHz mono 16 bits com medidor de nível.
/// Fase 2: ganho automático leve + passa-alta + VAD virão sobre este fluxo.
/// </summary>
public sealed class CaptadorMicrofone : IDisposable
{
    private WasapiCapture? _captura;
    private float _nivelAtual;

    public event EventHandler<float>? NivelMudou;

    public float NivelAtual => _nivelAtual;
    public bool Captando => _captura is not null;

    public IReadOnlyList<string> ListarDispositivos()
    {
        using var dispositivos = new MMDeviceEnumerator();
        return dispositivos.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active)
            .Select(d => d.FriendlyName).ToList();
    }

    public void Iniciar(int dispositivo = 0)
    {
        Parar();
        _captura = new WasapiLoopbackCapture();
        _captura.DataAvailable += AoReceberAudio;
        _captura.StartRecording();
    }

    private void AoReceberAudio(object? sender, WaveInEventArgs e)
    {
        // Calcula o pico do buffer (16 bits) para o medidor de volume ao vivo.
        float pico = 0;
        for (int i = 0; i + 1 < e.BytesRecorded; i += 2)
        {
            short amostra = (short)(e.Buffer[i] | (e.Buffer[i + 1] << 8));
            float v = Math.Abs(amostra / 32768f);
            if (v > pico) pico = v;
        }
        _nivelAtual = pico;
        NivelMudou?.Invoke(this, pico);
    }

    public void Parar()
    {
        _captura?.StopRecording();
        _captura?.Dispose();
        _captura = null;
        _nivelAtual = 0;
    }

    public void Dispose() => Parar();
}
