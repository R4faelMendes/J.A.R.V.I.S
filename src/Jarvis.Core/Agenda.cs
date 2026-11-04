namespace Jarvis.Core;

public enum EtapaCompromisso { Planejado, EmAndamento, Concluido }
public sealed record Compromisso(string Id, string Titulo, DateTimeOffset Inicio, DateTimeOffset Fim, int AvisoMinutos = 15, string Notas = "", long Versao = 0, bool Excluido = false, EtapaCompromisso Etapa = EtapaCompromisso.Planejado)
{
    public void Validar()
    {
        if (!Enum.IsDefined(Etapa) || !Guid.TryParse(Id, out _) || string.IsNullOrWhiteSpace(Titulo) || Titulo.Length > 160 || Notas is null || Notas.Length > 4000 || Inicio.Year is < 2000 or > 2100 || Fim <= Inicio || Fim - Inicio > TimeSpan.FromDays(31) || AvisoMinutos is < -1 or > 10080 || Versao < 0)
            throw new ArgumentException("Informe título, horário final posterior ao início e lembrete válido.");
    }
    public DateTimeOffset? AlertaEm => AvisoMinutos < 0 || Excluido || Etapa == EtapaCompromisso.Concluido ? null : Inicio.AddMinutes(-AvisoMinutos);
}
public static class AgendaWidget
{
    public static IReadOnlyList<Compromisso> Proximos(IEnumerable<Compromisso> itens, DateTimeOffset agora, int limite = 4) => itens
        .Where(c => !c.Excluido && c.Etapa != EtapaCompromisso.Concluido && c.Fim >= agora).OrderBy(c => c.Inicio).Take(Math.Clamp(limite, 1, 10)).ToArray();
}
public interface IAgendaService
{
    IReadOnlyList<Compromisso> Listar();
    Compromisso Salvar(Compromisso compromisso);
}
public interface IAcessoExterno { string? Endereco { get; } void Salvar(string endereco); void AbrirPagina(string url) => throw new NotSupportedException(); }
public sealed class ConflitoAgendaException() : InvalidOperationException("Este compromisso foi alterado em outro aparelho. Atualize a agenda antes de salvar novamente.");

public static class PosicoesGrade
{
    public static string[] Nomes(string modelo) => modelo switch
    {
        "Metade" => ["Esquerda", "Direita"], "TresColunas" => ["Esquerda", "Centro", "Direita"],
        "FocoLateral" => ["Principal · esquerda", "Lateral · direita"], "Grade2x2" => ["Superior esquerda", "Superior direita", "Inferior esquerda", "Inferior direita"],
        _ => throw new ArgumentException("Grade inválida.")
    };
    public static IReadOnlyList<RegiaoJanela> Calcular(string modelo, RegiaoJanela area, int quantidade, int[]? posicoes)
    {
        if (posicoes is null) return GradeJanelas.Calcular(modelo, area, quantidade);
        var capacidade = GradeJanelas.Capacidade(modelo);
        if (posicoes.Length != quantidade || quantidade < 1 || posicoes.Distinct().Count() != quantidade || posicoes.Any(p => p < 0 || p >= capacidade)) throw new ArgumentException("Escolha posições diferentes dentro da grade para cada janela.");
        var cells = GradeJanelas.Calcular(modelo, area, capacidade);
        return posicoes.Select(p => cells[p]).ToArray();
    }
}
