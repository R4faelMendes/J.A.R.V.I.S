using Jarvis.Voice;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
namespace Jarvis.Tests;
public sealed class EscutaNovaTest
{
    [Theory]
    [InlineData("jarbas abrir calculadora")]
    [InlineData("javis abrir calculadora")]
    [InlineData("já vis, abrir calculadora")]
    public void AtivacaoAceitaVariantesSemCortarComando(string heard)
    {
        Assert.True(WakeWordMatcher.ContainsWakeWord(heard, "Jarvis"));
        Assert.Contains("abrir calculadora", WakeWordMatcher.StripWakeWord(heard, "Jarvis"));
    }
    [Fact]
    public void SegmentadorPreservaInicioEAguardaPausa()
    {
        var c = new CapturadorComando(); var bloco = new byte[3200];
        c.Alimentar(bloco, true); c.Iniciar(); byte[]? ready = null;
        for (var i = 0; i < 7; i++) Assert.Null(c.Alimentar(bloco, true));
        for (var i = 0; i < 8; i++) Assert.Null(c.Alimentar(bloco, false));
        for (var i = 0; i < 4; i++) ready ??= c.Alimentar(bloco, false);
        Assert.NotNull(ready); Assert.True(ready!.Length > 32000); Assert.False(c.Ativo);
    }
    [Fact]
    public void GanhoAjudaFalaBaixaSemInventarSomNoSilencio()
    {
        var p = new PreparadorAudio(); Assert.All(p.Processar(new byte[3200]), b => Assert.Equal(0, b));
        var audio = new byte[3200]; for (var i = 0; i < 1600; i++) BitConverter.GetBytes((short)(300 * Math.Sin(i * .15))).CopyTo(audio, i * 2);
        var saida = p.Processar(audio); Assert.True(p.Fala); Assert.Contains(saida.Where((_, i) => i % 2 == 1), b => b != 0);
    }
    [Fact]
    public async Task WhisperRealEntendePcm16SemCabecalhoWav()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory); while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "models"))) dir = dir.Parent;
        Assert.NotNull(dir); var models = Path.Combine(dir!.FullName, "models"); var output = Path.Combine(dir.FullName, "artifacts", "voice", "comando-escuta.wav"); Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        using var piper = new PiperTts(models); await piper.SintetizarAsync("Jarvis, abrir a calculadora.", output);
        using var wav = new AudioFileReader(output); ISampleProvider mono = wav.WaveFormat.Channels == 2 ? new StereoToMonoSampleProvider(wav) : wav;
        var resampler = new WdlResamplingSampleProvider(mono, 16000); var buffer = new float[16000]; using var pcm = new MemoryStream(); int n;
        while ((n = resampler.Read(buffer, 0, buffer.Length)) > 0) for (var i = 0; i < n; i++) { var s = (short)(Math.Clamp(buffer[i], -1, 1) * short.MaxValue); pcm.Write(BitConverter.GetBytes(s)); }
        using var whisper = new DictationService(Path.Combine(models, File.Exists(Path.Combine(models, "ggml-base.bin")) ? "ggml-base.bin" : "ggml-tiny.bin"));
        var text = await whisper.TranscreverAsync(pcm.ToArray()); await File.WriteAllTextAsync(Path.Combine(dir.FullName, "artifacts", "voice", "whisper-resultado.txt"), text);
        Assert.Contains("calculadora", text, StringComparison.OrdinalIgnoreCase);
        Assert.True(WakeWordMatcher.AtivacaoNoInicio(text, "Jarvis"), text);
        var comando = WakeWordMatcher.StripWakeWord(text, "Jarvis");
        Assert.Equal("open_app", Jarvis.Ai.RoteadorIntencoes.Rotear(comando)?.Acao);
    }
    [Theory]
    [InlineData("coloque o volume em cinquenta por cento", "set_volume", "50")]
    [InlineData("volume trinta e cinco", "set_volume", "35")]
    [InlineData("você pode abrir a calculadora pra mim por favor", "open_app", "calculadora")]
    [InlineData("abri-la, calculadora", "open_app", "calculadora")]
    public void ComandosNaturaisComNumerosEFalasEducadas(string texto, string acao, string alvo)
    {
        var pedido = Jarvis.Ai.RoteadorIntencoes.Rotear(texto); Assert.Equal(acao, pedido?.Acao); Assert.Equal(alvo, pedido?.Alvo);
    }
}
