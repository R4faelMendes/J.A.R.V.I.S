// JARVIS.Tests — Fase 5: o interpretador de comandos (núcleo dos testes).
using Jarvis.Ai;

namespace Jarvis.Tests;

public sealed class RoteadorTest
{
    [Theory]
    [InlineData("abre o chrome", "abrir_app", "chrome")]
    [InlineData("abrir spotify", "abrir_app", "spotify")]
    [InlineData("fecha o discord", "fechar_app", "discord")]
    [InlineData("volume para 30", "volume", "30")]
    [InlineData("que horas são?", "hora", null)]
    [InlineData("que dia é hoje?", "data", null)]
    [InlineData("pare", "parar", null)]
    [InlineData("pausa a música", "midia", null)]
    [InlineData("como está meu pc?", "desempenho", null)]
    [InlineData("boa tarde", "saudacao", null)]
    public void Comandos_Deterministicos(string texto, string tipo, string? alvo)
    {
        var r = RoteadorIntencoes.Rotear(texto);
        Assert.NotNull(r);
        Assert.Equal(tipo, r.Tipo);
        if (alvo is not null) Assert.Contains(alvo, r.Alvo);
    }

    [Theory]
    [InlineData("explique buracos negros")]
    [InlineData("conte uma piada")]
    public void Abertas_VaoParaIA(string texto)
    {
        Assert.Null(RoteadorIntencoes.Rotear(texto));
    }

    [Fact]
    public async Task SemProvedor_ExplicaCapacidadesLocais()
    {
        var chat = new ChatService();
        var r = await chat.ResponderAsync("explique buracos negros");
        Assert.Contains("modo local", r);
    }

    [Fact]
    public async Task Roteador_NaoGastaProvedor()
    {
        var chat = new ChatService();
        var antes = chat.UsadasHoje;
        var r = await chat.ResponderAsync("que horas são?");
        Assert.Contains("horas", r);
        Assert.Equal(antes, chat.UsadasHoje);
    }
}
