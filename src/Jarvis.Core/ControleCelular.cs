namespace Jarvis.Core;

public sealed record EntradaCelular(Guid Sessao, string Acao, int Largura, int Altura, int Rotacao = 0,
    double X = .5, double Y = .5, double DestinoX = .5, double DestinoY = .5, int DuracaoMs = 180, string? Texto = null, string? Tecla = null)
{
    public void Validar()
    {
        static bool Unidade(double n) => double.IsFinite(n) && n is >= 0 and <= 1;
        if (Sessao == Guid.Empty || Acao is not ("tocar" or "segurar" or "arrastar" or "rolar" or "texto" or "tecla" or "voltar" or "inicio" or "recentes") ||
            Largura is < 1 or > 1920 || Altura is < 1 or > 1920 || Rotacao is < 0 or > 3 ||
            !Unidade(X) || !Unidade(Y) || !Unidade(DestinoX) || !Unidade(DestinoY) || DuracaoMs is < 50 or > 1200 ||
            Acao == "texto" && (string.IsNullOrEmpty(Texto) || Texto.Length > 1000 || Texto.Any(c => char.IsControl(c) && c is not ('\n' or '\t'))) ||
            Acao != "texto" && Texto is not null || Acao == "tecla" && Tecla is not ("Enter" or "Backspace" or "Tab" or "Escape" or "Left" or "Right" or "Up" or "Down"))
            throw new ArgumentException("Entrada inválida para o celular.");
    }
}
public sealed record EntradaCelularPendente(Guid Id, EntradaCelular Entrada, DateTimeOffset Expira);
public sealed record ConfirmacaoEntradaCelular(Guid Id, string Estado);
