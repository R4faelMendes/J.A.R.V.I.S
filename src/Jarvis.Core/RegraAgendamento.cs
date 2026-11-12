using System.Globalization;
namespace Jarvis.Core;

public static class RegraAgendamento
{
    public static void Validar(AgendamentoRotina item)
    {
        if (item.RotinaId < 1 || !TimeOnly.TryParseExact(item.Hora, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ||
            item.Dias is null || item.Dias.Length is < 1 or > 7 || item.Dias.Distinct().Count() != item.Dias.Length || item.Dias.Any(d => !Enum.IsDefined(d)))
            throw new ArgumentException("Escolha uma rotina, horário HH:mm e ao menos um dia válido.");
    }
    public static string Chave(DateTimeOffset agora) => agora.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    public static bool Devido(AgendamentoRotina item, DateTimeOffset agora) => item.Ativo && item.Dias.Contains(agora.DayOfWeek) &&
        item.Hora == agora.ToString("HH:mm", CultureInfo.InvariantCulture) && item.UltimaExecucao != Chave(agora);
}
