using Jarvis.Voice;
namespace Jarvis.Tests;

public sealed class PreparacaoVozTest
{
    [Fact]
    public async Task InstalacaoAusenteNaoAlegaVozProntaENaoIniciaBinarios()
    {
        var pasta = Path.Combine(Path.GetTempPath(), "jarvis-setup-test-" + Guid.NewGuid().ToString("N"));
        var setup = new PreparacaoVozService(pasta);
        Assert.Equal(4, setup.Verificar().Count);
        Assert.All(setup.Verificar(), item => Assert.False(item.Pronto));
        await Assert.ThrowsAsync<InvalidOperationException>(() => setup.TestarAsync(default));
        Assert.False(Directory.Exists(pasta));
    }
}
