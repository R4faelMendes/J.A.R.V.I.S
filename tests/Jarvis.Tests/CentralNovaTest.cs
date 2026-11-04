using Jarvis.Core;
using Jarvis.Data;
using Jarvis.Desktop.Services;
namespace Jarvis.Tests;
public sealed class CentralNovaTest
{
    [Fact]
    public void WakeOnLan_PacotePossuiSincronizacaoEMacRepetido()
    {
        var packet = WakeOnLan.CriarPacote("AA:BB:CC:DD:EE:FF"); Assert.Equal(102, packet.Length);
        Assert.All(packet.Take(6), b => Assert.Equal(255, b));
        for (var i = 0; i < 16; i++) Assert.Equal(Convert.FromHexString("AABBCCDDEEFF"), packet.Skip(6 + i * 6).Take(6));
        Assert.Throws<ArgumentException>(() => WakeOnLan.CriarPacote("not-a-real-mac"));
    }
    [Theory]
    [InlineData("Jarvis, próxima música", "midia_proxima")]
    [InlineData("volume 40", "volume")]
    [InlineData("reiniciar PC", "reiniciar")]
    public void VozRemota_TraduzSomenteListaFechada(string texto, string comando) => Assert.Equal(comando, ComandoRemotoVoz.Traduzir(texto)?.Nome);
    [Theory]
    [InlineData("execute powershell -command remove-item")]
    [InlineData("volume 120")]
    [InlineData("reiniciar pc e apagar arquivos")]
    public void VozRemota_RejeitaTextoLivre(string texto) => Assert.Null(ComandoRemotoVoz.Traduzir(texto));
    [Fact]
    public void Memoria_ExigeOptInPersisteEPermiteApagar()
    {
        var folder = Path.Combine(Path.GetTempPath(), "jarvis-memory-" + Guid.NewGuid()); var file = Path.Combine(folder, "memoria.json");
        try
        {
            var memory = new MemoriaPreferencias(file); Assert.False(memory.Ativa);
            Assert.Throws<InvalidOperationException>(() => memory.Salvar("aplicativo favorito", "Spotify"));
            memory.Ativa = true; memory.Salvar("aplicativo favorito", "Spotify");
            var restored = new MemoriaPreferencias(file); Assert.True(restored.Ativa); Assert.Equal("Spotify", Assert.Single(restored.Listar()).Valor);
            restored.Apagar("aplicativo favorito"); Assert.Empty(new MemoriaPreferencias(file).Listar());
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
    [Theory]
    [InlineData("[{\"Comando\":\"reiniciar\"}]")]
    [InlineData("[{\"Comando\":\"volume\",\"Argumento\":\"200\"}]")]
    [InlineData("[{\"Comando\":\"layout\",\"Argumento\":\"qualquer\"}]")]
    [InlineData("[]")]
    public void Rotinas_ValidamAntesDeExecutar(string json) => Assert.Throws<ArgumentException>(() => RotinaService.Validar(json));
    [Fact]
    public async Task Rotina_CanceladaNaoExecutaProximaEtapa()
    {
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new RotinaService().ExecutarAsync("[{\"Comando\":\"esperar\",\"Argumento\":\"1\"}]", new Progress<ProgressoRotina>(), cts.Token));
    }
}
