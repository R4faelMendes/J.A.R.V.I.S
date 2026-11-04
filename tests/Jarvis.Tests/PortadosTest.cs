// Testes do código portado de Jarvis_test (clima, notícias, sanitizador, wake tolerante).
using Jarvis.Info;
using Jarvis.Voice;

namespace Jarvis.Tests;

public sealed class PortadosTest
{
    [Fact]
    public void Sanitizer_HorasDatasValores()
    {
        Assert.Equal("9 horas", TtsTextSanitizer.Sanitize("09:00"));
        Assert.Equal("14 e 30", TtsTextSanitizer.Sanitize("14:30"));
        Assert.Equal("16 de julho", TtsTextSanitizer.Sanitize("16/07"));
        Assert.Equal("50 por cento", TtsTextSanitizer.Sanitize("50%"));
        Assert.Equal("1200 reais", TtsTextSanitizer.Sanitize("R$ 1200"));
        Assert.Equal("veja link", TtsTextSanitizer.Sanitize("veja https://exemplo.com/x"));
        Assert.Equal("", TtsTextSanitizer.Sanitize(""));
    }

    [Fact]
    public void WakeMatcher_ExatoEAproximado()
    {
        Assert.True(WakeWordMatcher.ContainsWakeWord("oi jarvis tudo bem", "jarvis"));
        Assert.True(WakeWordMatcher.ContainsWakeWord("oi jarvi tudo bem", "jarvis")); // 1 erro ok
        Assert.True(WakeWordMatcher.ContainsWakeWord("ei Jarvis, abre o chrome", "ei jarvis"));
        Assert.False(WakeWordMatcher.ContainsWakeWord("bom dia", "jarvis"));
        Assert.False(WakeWordMatcher.ContainsWakeWord("", "jarvis"));
    }

    [Fact]
    public void WakeMatcher_RemoveWakeDoInicio()
    {
        Assert.Equal("abre o chrome", WakeWordMatcher.StripWakeWord("jarvis abre o chrome", "jarvis"));
        Assert.Equal("tudo bem por aqui", WakeWordMatcher.StripWakeWord("tudo bem por aqui", "jarvis"));
    }

    [Fact]
    public void Noticias_LimpaSufixoDaFonte()
    {
        Assert.Equal("Acordo assinado", NewsService.LimparTitulo("Acordo assinado - G1", "G1"));
        Assert.Equal("Sem fonte", NewsService.LimparTitulo("Sem fonte", ""));
        Assert.Equal("", NewsService.LimparTitulo("   ", "X"));
    }

    [Fact]
    public async Task Clima_CidadeInexistente_RetornaNulo()
    {
        var r = await WeatherService.ObterAsync("XyzCidadeQueNaoExiste123");
        Assert.Null(r);
    }
}
