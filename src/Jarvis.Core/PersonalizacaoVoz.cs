namespace Jarvis.Core;

public static class PersonalizacaoVoz
{
    public static string Resposta(string texto, string? nome)
    {
        nome = LimparNome(nome);
        if (nome.Length == 0 || string.IsNullOrWhiteSpace(texto) || texto.Contains(nome, StringComparison.OrdinalIgnoreCase)) return texto;
        return $"{nome}, {texto}";
    }
    public static string Saudacao(string? nome, int hora) => Resposta((hora < 12 ? "Bom dia" : hora < 18 ? "Boa tarde" : "Boa noite") + ". Estou pronto para ajudar. Diga Jarvis e seu comando.", nome);
    private static string LimparNome(string? nome) => string.Join(" ", (nome ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Trim();
}
