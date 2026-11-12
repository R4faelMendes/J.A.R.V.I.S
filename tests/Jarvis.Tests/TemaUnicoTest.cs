using Jarvis.Core;
namespace Jarvis.Tests;

public sealed class TemaUnicoTest
{
    [Fact]
    public void IdentidadeUnicaTemContrasteEMapeamentoIdempotente()
    {
        var paleta = Assert.Single(TemasJarvis.Catalogo);
        Assert.Equal("#000000", paleta.Fundo);
        Assert.Equal("#00D800", paleta.Acento);
        Assert.Equal("#FFFFFF", Theme.Gelo);
        foreach (var rgb in new uint[] { 0x4FD6FF, 0x82E0E6, 0x00D9CF, 0x35F29A })
            Assert.Equal(0x00D800u, TemasJarvis.Colorir(rgb));
        foreach (var rgb in new uint[] { 0x000000, 0x101010, 0x333333, 0xFFFFFF, 0x00D800, 0x102235, 0x8FA9BF, 0xFF4D3D }) {
            var mapped = TemasJarvis.Colorir(rgb);
            Assert.Equal(mapped, TemasJarvis.Colorir(mapped));
        }
        // Verde e branco superam 4,5:1 sobre o painel; texto no botão verde deve ser preto.
        double Lum(uint rgb) {
            double F(uint n) { var v = n / 255d; return v <= .04045 ? v / 12.92 : Math.Pow((v + .055) / 1.055, 2.4); }
            return .2126 * F(rgb >> 16 & 255) + .7152 * F(rgb >> 8 & 255) + .0722 * F(rgb & 255);
        }
        Assert.True((Lum(0x00D800) + .05) / (Lum(0x101010) + .05) > 4.5);
        Assert.True((Lum(0x00D800) + .05) / .05 > 4.5);
    }

    [Theory]
    [InlineData("azul")][InlineData("ambar")][InlineData("violeta")]
    [InlineData("esmeralda")][InlineData("prata")][InlineData("rubi")]
    public void ClienteAntigoNaoReativaPaletaRemovida(string id)
    {
        Assert.Equal("jarvis", TemasJarvis.Sincronizar(new(id, 100)).Id);
        Assert.Equal("jarvis", TemasJarvis.Escolher(id).Id);
        Assert.Single(TemasJarvis.Catalogo);
    }
}
