// JARVIS.Tests — Fases 6/8/9: permissões, banco, comandos fixos.
using Jarvis.Data;
using Jarvis.Server;

namespace Jarvis.Tests;

public sealed class PermissoesTest
{
    [Fact]
    public void Risco_ConfirmaPorPadrao()
    {
        var p = new Permissoes();
        Assert.True(p.PodeExecutar(TipoAcao.ReiniciarPc, out var confirma) && confirma);
        Assert.True(p.PodeExecutar(TipoAcao.FecharApp, out confirma) && confirma);
        Assert.True(p.PodeExecutar(TipoAcao.AbrirApp, out confirma) && !confirma);
    }
}

public sealed class BancoTest : IDisposable
{
    private readonly BancoJarvis _banco = new();
    public void Dispose() => _banco.Dispose();

    [Fact]
    public void RotinasPadrao_Existem()
    {
        Assert.True(_banco.ListarRotinas().Count >= 3);
    }

    [Fact]
    public void Acao_RegistraELista()
    {
        _banco.RegistrarAcao("abrir chrome", "voz", "ok");
        Assert.Contains(_banco.ListarAcoes(5), a => a.Comando == "abrir chrome");
    }

    [Fact]
    public void Rotina_Alterna()
    {
        var r = _banco.ListarRotinas()[0];
        _banco.AlternarRotina(r.Id, !r.Ativa);
        Assert.Equal(!r.Ativa, _banco.ListarRotinas().First(x => x.Id == r.Id).Ativa);
        _banco.AlternarRotina(r.Id, r.Ativa);
    }
}

public sealed class ServidorTest
{
    [Fact]
    public void ListaFixa_BloqueiaTextoLivre()
    {
        Assert.DoesNotContain("executar_texto", ComandosFixos.Permitidos);
        Assert.Contains("abrir_app", ComandosFixos.Permitidos);
        Assert.DoesNotContain("formatar", ComandosFixos.Permitidos);
    }

    [Fact]
    public void Token_UsoUnicoEExpira()
    {
        var p = new PareamentoService();
        var t = p.EmitirToken();
        Assert.True(p.ValidarToken(t));
        Assert.False(p.ValidarToken(t)); // uso único
    }

    [Fact]
    public void Qr_TemQuatroPartes()
    {
        var qr = PareamentoService.MontarQr("192.168.0.2", 8766, "tok", "ABC123");
        Assert.StartsWith("jarvis://parear?", qr);
        Assert.Contains("pin=ABC123", qr);
    }
}
