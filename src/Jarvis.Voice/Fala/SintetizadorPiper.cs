namespace Jarvis.Voice.Fala;

/// <summary>
/// TTS Piper pt-BR local. Processo/modelo mantido quente para falar em ~300 ms;
/// cache de frases frequentes; reprodução em fluxo via NAudio.
/// Modelo em %LOCALAPPDATA%\Jarvis\modelos\piper-pt-BR.onnx (bootstrap baixa).
/// </summary>
public sealed class SintetizadorPiper : IDisposable
{
    private readonly Dictionary<string, byte[]> _cache = new();
    public float Velocidade { get; set; } = 1.0f;
    public string Voz { get; set; } = "Atlas (faber-medium pt-BR)";

    public Task FalarAsync(string texto, CancellationToken ct = default)
    {
        // Fase 2 final: piper.exe --model ... --speed + NAudio em fluxo.
        return Task.CompletedTask;
    }

    public void Dispose() { }
}
