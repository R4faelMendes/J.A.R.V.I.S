using Jarvis.Core;
namespace Jarvis.Tests;
public sealed class Revisao201Test
{
    [Theory]
    [InlineData("Rafael", "Rafael, Calculadora aberta.")]
    [InlineData("Ana Maria", "Ana Maria, Calculadora aberta.")]
    [InlineData("", "Calculadora aberta.")]
    public void RespostaUsaNomeConfigurado(string nome, string esperado) => Assert.Equal(esperado, PersonalizacaoVoz.Resposta("Calculadora aberta.", nome));
    [Fact] public void NaoRepeteNomeEIncluiNaSaudacao() {
        Assert.Equal("Olá, Rafael.", PersonalizacaoVoz.Resposta("Olá, Rafael.", "Rafael"));
        Assert.Contains("Rafael", PersonalizacaoVoz.Saudacao("Rafael", 20));
        Assert.Contains("Boa noite", PersonalizacaoVoz.Saudacao("Rafael", 20));
    }
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void AneisMantemRaioEPlanoDuranteGiro(int anel) {
        var normal = ReatorCinematica.Posicao(anel, 0, 0, 0, 1);
        for (var t = 0d; t < 90; t += .25) {
            var p = ReatorCinematica.Posicao(anel, t, .5, .8, .03);
            Assert.Equal(.8 * .8 + .03 * .03, p.X * p.X + p.Y * p.Y + p.Z * p.Z, 10);
            Assert.Equal(.03, p.X * normal.X + p.Y * normal.Y + p.Z * normal.Z, 10);
            var next = ReatorCinematica.Posicao(anel, t + 1d / 60, .5, .8, .03);
            Assert.InRange(Math.Sqrt(Math.Pow(next.X - p.X, 2) + Math.Pow(next.Y - p.Y, 2) + Math.Pow(next.Z - p.Z, 2)), .0001, .003);
        }
    }
    [Fact] public void TemaCobreAzulECianoEPreservaEstadosETextoNeutro() {
        var anterior = TemasJarvis.Estado;
        try {
            foreach (var paleta in TemasJarvis.Catalogo) {
                TemasJarvis.Escolher(paleta.Id);
                Assert.Equal(TemasJarvis.Rgb(paleta.Acento), TemasJarvis.Colorir(0x4FD6FF));
                Assert.NotEqual(0x00D9CFu, TemasJarvis.Colorir(0x00D9CF));
                Assert.Equal(0xFF4D3Du, TemasJarvis.Colorir(0xFF4D3D));
                Assert.Equal(0x00D800u, TemasJarvis.Colorir(0x4DE3A1));
                Assert.Equal(0xFFFFFFu, TemasJarvis.Colorir(0xE6F6FF));
                // Contraste do texto sobre as superfícies escuras.
                double Lum(uint c) { double F(int n) { var v = n / 255d; return v <= .04045 ? v / 12.92 : Math.Pow((v + .055) / 1.055, 2.4); } return .2126 * F((int)(c >> 16 & 255)) + .7152 * F((int)(c >> 8 & 255)) + .0722 * F((int)(c & 255)); }
                Assert.True((Lum(0xE6F6FF) + .05) / (Lum(TemasJarvis.Rgb(paleta.Painel)) + .05) >= 4.5);
            }
        } finally { TemasJarvis.Sincronizar(anterior); }
    }
}
