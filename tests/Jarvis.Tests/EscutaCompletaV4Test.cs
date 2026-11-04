using Jarvis.Core;
using Jarvis.Voice;
using Jarvis.Desktop.Services;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
namespace Jarvis.Tests;
public sealed class EscutaCompletaV4Test
{
    [Fact]
    public void HeadsetQueParaDeEnviarSilencioAindaFinalizaComando()
    { var c = new CapturadorComando(); c.Iniciar(); c.Alimentar(new byte[3200], true); var audio = c.ExtrairPorInatividade(DateTime.UtcNow.AddSeconds(2)); Assert.NotNull(audio); Assert.False(c.Ativo); Assert.Null(c.ExtrairPorInatividade(DateTime.UtcNow.AddSeconds(3))); }
    [Theory]
    [InlineData("", "jarvis abre a calculadora", true)]
    [InlineData("incompreensivel", "jarvis abrir calculadora", true)]
    [InlineData("", "abra a calculadora", false)]
    public void AlternativaLocalNaoDependeDeWhisper(string whisper, string vosk, bool wake)
    { var t = EscolhaTranscricao.Selecionar(whisper, vosk, "Jarvis", wake); Assert.Equal("open_app", Jarvis.Ai.RoteadorIntencoes.Rotear(WakeWordMatcher.StripWakeWord(t, "Jarvis"))?.Acao); }
    [Fact]
    public void AlternativaNaoAutorizaConversaAmbiente()
    { var t = EscolhaTranscricao.Selecionar("", "abrir calculadora", "Jarvis", true); Assert.False(WakeWordMatcher.AtivacaoNoInicio(t, "Jarvis")); }
    [Theory]
    [InlineData("Jarvis abre a calculadora por favor", "abrir_app", "calculadora")]
    [InlineData("Jarvis coloque o volume em cinquenta por cento", "volume", "50")]
    public void CelularAceitaFalaNatural(string texto, string acao, string alvo)
    { var c = ComandoRemotoVoz.Traduzir(texto); Assert.Equal(acao, c?.Nome); Assert.Equal(alvo, c?.Argumento); }
    private sealed class AplicativosTeste : IAppController
    {
        public string? Aberto;
        public IReadOnlyList<AppInfo> ListarApps() => new[] { new AppInfo("calc", "Calculadora", "calculadora", false, false) };
        public void Abrir(string id) => Aberto = id;
        public void Fechar(string id) { }
        public void AdicionarApp(string n, string e, string a, bool jogo = false) { }
    }
    [Fact]
    public async Task AudioPassaPorVoskSegmentacaoWhisperEAberturaLocal()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory); while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Jarvis.sln"))) dir = dir.Parent;
        var models = Path.Combine(dir!.FullName, "models"); var arq = Path.Combine(dir.FullName, "artifacts", "voice", "escuta-completa-v4.wav"); Directory.CreateDirectory(Path.GetDirectoryName(arq)!);
        using (var piper = new PiperTts(models)) await piper.SintetizarAsync("Jarvis, abra a calculadora por favor.", arq);
        using var mic = new VoiceService(); using var wake = new WakeWordListener(mic); wake.Carregar(Path.Combine(models, "vosk-model-small-pt-0.3"));
        SegmentoVoz? segmento = null; wake.SegmentoPronto += (_, s) => segmento ??= s;
        using var wav = new AudioFileReader(arq); var samples = new WdlResamplingSampleProvider(wav, 16000); var b = new float[1600]; var filtro = new PreparadorAudio(); int n;
        for (var i = 0; i < 5; i++) wake.ProcessarPcm(new byte[3200], false);
        while ((n = samples.Read(b, 0, b.Length)) > 0)
        { var pcm = new byte[n * 2]; for (var i = 0; i < n; i++) BitConverter.GetBytes((short)(Math.Clamp(b[i], -1, 1) * 32767)).CopyTo(pcm, i * 2); pcm = filtro.Processar(pcm); wake.ProcessarPcm(pcm, filtro.Fala); }
        for (var i = 0; i < 45 && segmento is null; i++) wake.ProcessarPcm(new byte[3200], false);
        Assert.NotNull(segmento);
        using var ditado = new DictationService(Path.Combine(models, "ggml-base.bin")); var texto = await ditado.TranscreverAsync(segmento!.Audio);
        texto = EscolhaTranscricao.Selecionar(texto, segmento.TextoVosk, "Jarvis", segmento.VerificarAtivacao);
        if (segmento.VerificarAtivacao) Assert.True(WakeWordMatcher.AtivacaoNoInicio(texto, "Jarvis"), texto);
        var command = WakeWordMatcher.StripWakeWord(texto, "Jarvis"); var apps = new AplicativosTeste();
        using var motor = new MotorVoz(models, apps: apps);
        var resposta = await motor.ExecutarComandoAsync(command, false);
        Assert.Equal("calc", apps.Aberto); Assert.Contains("Abrindo", resposta);
    }
}
