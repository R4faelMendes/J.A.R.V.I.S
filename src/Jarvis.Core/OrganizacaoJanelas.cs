namespace Jarvis.Core;

public sealed record MonitorInfo(string Id, string Nome, int X, int Y, int Largura, int Altura, bool Principal);
public sealed record PedidoLayout(string Modelo, string? MonitorId = null, long[]? Janelas = null, int[]? Posicoes = null);
public sealed record ResultadoLayout(int Movidas, IReadOnlyList<string> Avisos);
public readonly record struct RegiaoJanela(int X, int Y, int Largura, int Altura);
public static class GradeJanelas
{
    public static int Capacidade(string modelo) => modelo switch
    { "Metade" or "FocoLateral" => 2, "TresColunas" => 3, "Grade2x2" => 4, _ => throw new ArgumentException("Layout inválido.") };
    public static IReadOnlyList<RegiaoJanela> Calcular(string modelo, RegiaoJanela area, int quantidade, int espaco = 8)
    {
        var capacidade = Capacidade(modelo);
        if (area.Largura < 100 || area.Altura < 100 || quantidade < 1 || quantidade > capacidade || espaco is < 0 or > 32)
            throw new ArgumentException($"Escolha de 1 a {capacidade} janelas e um monitor válido.");
        var resultado = new List<RegiaoJanela>();
        for (var i = 0; i < quantidade; i++)
        {
            int x0, x1, y0 = 0, y1 = area.Altura;
            if (modelo == "Grade2x2")
            { x0 = area.Largura * (i % 2) / 2; x1 = area.Largura * (i % 2 + 1) / 2; y0 = area.Altura * (i / 2) / 2; y1 = area.Altura * (i / 2 + 1) / 2; }
            else if (modelo == "FocoLateral")
            { x0 = i == 0 ? 0 : area.Largura * 2 / 3; x1 = i == 0 ? area.Largura * 2 / 3 : area.Largura; }
            else { x0 = area.Largura * i / capacidade; x1 = area.Largura * (i + 1) / capacidade; }
            resultado.Add(new(area.X + x0 + espaco, area.Y + y0 + espaco, x1 - x0 - 2 * espaco, y1 - y0 - 2 * espaco));
        }
        return resultado;
    }
}

public sealed record QuadroTela(byte[] Jpeg, int Largura, int Altura, DateTimeOffset Instante);
public sealed record OpcoesTransmissao(int Fps = 60, int Largura = 1280, int Qualidade = 60)
{
    public void Validar()
    {
        if (Fps is not (15 or 30 or 60) || Largura is not (640 or 960 or 1280 or 1920) || Qualidade is < 35 or > 85)
            throw new ArgumentException("Escolha 15, 30 ou 60 FPS e uma resolução suportada.");
    }
}
public interface ISessaoTela : IDisposable { string Motor => "indisponível"; QuadroTela Capturar(); }
public interface ITransmissaoTela
{
    bool Permitida { get; set; }
    DateTimeOffset? UltimaVisualizacao { get; }
    QuadroTela Capturar();
    QuadroTela Capturar(string? monitorId) => Capturar();
    ISessaoTela AbrirSessao(string? monitorId, OpcoesTransmissao opcoes) => throw new InvalidOperationException("Atualize o Jarvis do PC para transmitir ao vivo.");
}
