namespace Jarvis.Core;

public static class AgendaDatas
{
    public static bool PodeSelecionar(DateTime data, DateTime hoje) => data.Date >= hoje.Date;
    public static void Validar(Compromisso item, DateTime hoje)
    {
        if (!item.Excluido && !PodeSelecionar(item.Inicio.LocalDateTime, hoje))
            throw new ArgumentException("Escolha hoje ou uma data futura. Não é possível agendar em dias que já passaram.");
    }
}

public static class BateriaAparelho
{
    public static int? Percentual(int nivel, int escala) => nivel < 0 || escala <= 0 || nivel > escala
        ? null : (int)Math.Round(nivel * 100d / escala, MidpointRounding.AwayFromZero);
}
