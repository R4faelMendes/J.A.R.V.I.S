using Jarvis.Server;
using SkiaSharp;
namespace Jarvis.Tests;
public sealed class QrNativoTest
{
    [Fact]
    public void QrGeradoPodeSerLidoPeloMesmoDecodificadorDoApp()
    {
        var link = PareamentoService.MontarQr("192.168.1.25", 5189, new string('A', 32), new string('B', 64)) + "&ips=192.168.1.25%2C192.168.0.25";
        using var bitmap = SKBitmap.Decode(PareamentoService.QrPng(link));
        var decoded = Jarvis.Mobile.Services.DecodificadorQr.Ler(bitmap);
        Assert.NotNull(decoded); Assert.Equal(link, decoded);
        var uri = new Uri(decoded); Assert.Equal("jarvis", uri.Scheme); Assert.Equal("parear", uri.Host);
    }
    [Theory]
    [InlineData("abrir calculadora")]
    [InlineData("estou falando sobre jarvis")]
    [InlineData("a musica do jarvis")]
    public void FalaSemAtivacaoInicialNaoDispara(string text) => Assert.False(Jarvis.Voice.WakeWordMatcher.AtivacaoNoInicio(text, "Jarvis"));
}
