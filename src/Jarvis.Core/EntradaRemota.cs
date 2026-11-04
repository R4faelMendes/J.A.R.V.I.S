namespace Jarvis.Core;

public sealed record PedidoToque(string MonitorId, string Acao, double X = .5, double Y = .5,
    double DestinoX = .5, double DestinoY = .5, int Rolagem = 0, int DuracaoMs = 350, string? Tecla = null)
{
    public void Validar()
    {
        static bool Unidade(double v) => double.IsFinite(v) && v is >= 0 and <= 1;
        if (string.IsNullOrWhiteSpace(MonitorId) || MonitorId.Length > 128 ||
            Acao is not ("clique" or "duplo" or "direito" or "arrastar" or "rolar" or "tecla") ||
            !Unidade(X) || !Unidade(Y) || !Unidade(DestinoX) || !Unidade(DestinoY) ||
            Rolagem is < -12 or > 12 || DuracaoMs is < 50 or > 1200 ||
            Acao == "rolar" && Rolagem == 0 || Acao == "tecla" && Tecla is not ("Enter" or "Escape" or "Tab" or "Backspace" or "Left" or "Right" or "Up" or "Down"))
            throw new ArgumentException("Gesto inválido. Atualize a transmissão e tente novamente.");
    }
}
public sealed record EstadoEntrada(bool Permitida, string Mensagem);
public interface IEntradaRemota
{
    bool Permitida { get; set; }
    Task ExecutarAsync(PedidoToque pedido, CancellationToken ct = default);
}
public readonly record struct PontoTela(double X, double Y);
public static class CoordenadasTela
{
    /// <summary>Ignora as faixas pretas de AspectFit; coordenadas independem da resolução transmitida.</summary>
    public static PontoTela? Normalizar(double x, double y, double largura, double altura, double quadroLargura, double quadroAltura)
    {
        if (new[] { x, y, largura, altura, quadroLargura, quadroAltura }.Any(v => !double.IsFinite(v)) || largura <= 0 || altura <= 0 || quadroLargura <= 0 || quadroAltura <= 0) return null;
        var escala = Math.Min(largura / quadroLargura, altura / quadroAltura);
        var w = quadroLargura * escala; var h = quadroAltura * escala;
        var nx = (x - (largura - w) / 2) / w; var ny = (y - (altura - h) / 2) / h;
        return nx is >= 0 and <= 1 && ny is >= 0 and <= 1 ? new(nx, ny) : null;
    }
    public static (int X, int Y) Pixel(PontoTela ponto, RegiaoJanela monitor) =>
        (monitor.X + (int)Math.Round(Math.Clamp(ponto.X, 0, 1) * (monitor.Largura - 1)), monitor.Y + (int)Math.Round(Math.Clamp(ponto.Y, 0, 1) * (monitor.Altura - 1)));
}
