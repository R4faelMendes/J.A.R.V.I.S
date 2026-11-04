// Correções pedidas pelo dono: apps reais, sem e-mail falso, QR real.
using Jarvis.Sistema;

namespace Jarvis.Tests;

public sealed class AppsReaisTest
{
    [Fact]
    public void Catalogo_TemCalculadoraENotepad()
    {
        var apps = new AppController().ListarApps();
        Assert.Contains(apps, a => a.Id == "calc");
        Assert.Contains(apps, a => a.Id == "notepad");
    }

    [Fact]
    public void FecharInexistente_ErroClaro()
    {
        Assert.ThrowsAny<Exception>(() => new AppController().Fechar("app-que-nao-existe-xyz"));
    }

    [Fact]
    public void FecharAppFechado_NaoEstoura()
    {
        // Calculadora fechada: Fechar não faz nada (antes, o nome errado impedia até a aberta de fechar).
        var ex = Record.Exception(() => new AppController().Fechar("calc"));
        Assert.Null(ex);
    }
}
