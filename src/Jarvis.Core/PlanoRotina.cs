using System.Text.Json;
namespace Jarvis.Core;
public static class PlanoRotina
{
    public static string Descrever(PassoRotina passo) => passo.Comando switch
    {
        "abrir_app" => "Abrir aplicativo", "volume" => $"Volume em {passo.Argumento}%", "midia_pausar" => "Pausar música", "midia_continuar" => "Continuar música", "layout" => "Organizar janelas", "esperar" => $"Esperar {passo.Argumento} segundos", _ => "Ação indisponível"
    };
    private static readonly HashSet<string> Permitidos = new() { "abrir_app", "volume", "midia_pausar", "midia_continuar", "layout", "esperar" };
    public static PassoRotina[] Validar(string json)
    {
        PassoRotina[] passos;
        try { passos = JsonSerializer.Deserialize<PassoRotina[]>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? []; }
        catch (JsonException) { throw new ArgumentException("A rotina antiga contém uma descrição. Crie uma nova sequência com ações executáveis."); }
        if (passos.Length is < 1 or > 20 || passos.Any(p => p is null || !Permitidos.Contains(p.Comando))) throw new ArgumentException("Use de 1 a 20 etapas válidas.");
        foreach (var p in passos)
        {
            if (p.Comando == "volume" && (!int.TryParse(p.Argumento, out var v) || v is < 0 or > 100)) throw new ArgumentException("Volume inválido.");
            if (p.Comando == "esperar" && (!int.TryParse(p.Argumento, out var t) || t is < 1 or > 30)) throw new ArgumentException("Espera deve ser de 1 a 30 segundos.");
            if (p.Comando == "layout" && p.Argumento is not ("Metade" or "TresColunas" or "FocoLateral" or "Grade2x2")) throw new ArgumentException("Layout inválido.");
            if (p.Comando == "abrir_app" && string.IsNullOrWhiteSpace(p.Argumento)) throw new ArgumentException("Selecione um aplicativo.");
        }
        return passos;
    }
}
