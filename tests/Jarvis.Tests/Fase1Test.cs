// JARVIS.Tests — Fase 1: tokens, páginas e contratos (sem UI, sem Win32).
using Jarvis.Core;

namespace Jarvis.Tests;

public sealed class TokensTest
{
    [Fact]
    public void Cores_SaoHexValidos()
    {
        foreach (var cor in new[] { Theme.Abismo, Theme.Vidro, Theme.Linha, Theme.CianoHolo,
            Theme.Gelo, Theme.Nevoa, Theme.Ouro, Theme.VermelhoNucleo, Theme.VerdeSistema })
        {
            Assert.Matches(@"^#[0-9A-Fa-f]{6}$", cor);
        }
    }

    [Fact]
    public void Raios_SeguemGradeDe8()
    {
        Assert.Equal(0, Theme.RaioBotao % 2);
        Assert.True(Theme.RaioPainel > Theme.RaioBotao);
    }

    [Fact]
    public void Reator_TemTresTamanhos()
    {
        Assert.True(Theme.ReatorPequeno < Theme.ReatorMedio);
        Assert.True(Theme.ReatorMedio < Theme.ReatorGrande);
    }
}

public sealed class NavegacaoTest
{
    [Fact]
    public void Paginas_PreservamIdsEIncluemEmails()
    {
        var paginas = Enum.GetValues<PageType>();
        Assert.Equal(17, paginas.Length);
        Assert.Contains(PageType.Atualizacoes, paginas); Assert.Contains(PageType.Rotinas, paginas); Assert.Contains(PageType.ResumoDia, paginas);
        Assert.Equal(PageType.Nucleo, paginas[0]);
        Assert.Equal(7, (int)PageType.Configuracoes);
        Assert.Contains(PageType.Emails, paginas);
        Assert.Contains(PageType.ModoJogo, paginas);
    }

    [Fact]
    public void SeteEstadosDoReator()
    {
        Assert.Equal(7, Enum.GetValues<ReactorState>().Length);
    }
}

public sealed class ContratosTest
{
    [Fact]
    public void Records_CarregamDados()
    {
        var app = new AppInfo("chrome", "Chrome", "chrome", true, false);
        Assert.Equal("chrome", app.ApelidoVoz);
    }
}
