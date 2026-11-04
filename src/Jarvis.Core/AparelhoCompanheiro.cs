namespace Jarvis.Core;

public sealed record DadosAparelho(string Modelo, string Sistema, int? Bateria, bool? Carregando,
    long? ArmazenamentoLivre, long? ArmazenamentoTotal, string Conexao, bool Vpn, bool ControleAtivo = false, bool ControleSuportado = true);
public sealed record AparelhoPainel(string Id, string Nome, DadosAparelho Dados, DateTimeOffset Visto,
    bool Online, bool Espelhando, string Localizacao, Guid? Sessao = null, string Controle = "");
public sealed record OrdemAparelho(Guid Id, string Acao, DateTimeOffset Expira);
public sealed record ConfirmacaoAparelho(Guid Id, string Estado);
public sealed record SessaoEspelho(Guid Id);
public sealed record ImagemAparelho(byte[] Jpeg, int Largura, int Altura, DateTimeOffset Instante, int Rotacao = 0);
public interface ICentralAparelhos
{
    IReadOnlyList<AparelhoPainel> Listar();
    void Encontrar(string id);
    void Silenciar(string id);
    void PararEspelho(string id);
    ImagemAparelho? Quadro(string id);
    void Controlar(string id, EntradaCelular entrada);
}
public static class JpegAparelho
{
    public const int LimiteBytes = 2 * 1024 * 1024;
    public static (int Largura, int Altura) Validar(ReadOnlySpan<byte> data)
    {
        if (data.Length is < 8 or > LimiteBytes || data[0] != 255 || data[1] != 216 || data[^2] != 255 || data[^1] != 217) throw new ArgumentException("Quadro JPEG inválido.");
        var i = 2;
        while (i + 3 < data.Length)
        {
            if (data[i++] != 255) throw new ArgumentException("Cabeçalho JPEG inválido.");
            while (i < data.Length && data[i] == 255) i++;
            if (i >= data.Length) break;
            var marker = data[i++];
            if (marker is 0xD9 or 0xDA) break;
            if (marker is >= 0xD0 and <= 0xD7 or 0x01) continue;
            if (i + 1 >= data.Length) break;
            var length = data[i] * 256 + data[i + 1];
            if (length < 2 || i + length > data.Length) break;
            if (marker is 0xC0 or 0xC1 or 0xC2)
            {
                if (length < 8) break;
                var h = data[i + 3] * 256 + data[i + 4]; var w = data[i + 5] * 256 + data[i + 6];
                if (w < 1 || h < 1 || w > 1920 || h > 1920 || (long)w * h > 3_000_000) break;
                return (w, h);
            }
            i += length;
        }
        throw new ArgumentException("Resolução do quadro inválida ou acima do limite.");
    }
}
