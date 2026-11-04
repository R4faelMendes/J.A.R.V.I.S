using Jarvis.Core;
namespace Jarvis.Ai;
public static class ResolverAplicativo
{
    private static readonly Dictionary<string, string> Apelidos = new()
    {
        ["crome"] = "chrome", ["crom"] = "chrome", ["crômio"] = "chrome",
        ["espotifai"] = "spotify", ["espotify"] = "spotify", ["spot fai"] = "spotify",
        ["discor"] = "discord", ["dis corde"] = "discord",
        ["estime"] = "steam", ["stim"] = "steam", ["visual studio code"] = "vs code",
        ["vscode"] = "vs code", ["bloco"] = "bloco de notas", ["calculador"] = "calculadora"
    };
    public static AppInfo? Encontrar(IEnumerable<AppInfo> apps, string pedido, bool aproximar = false)
    {
        var nome = RoteadorIntencoes.Normalizar(pedido);
        if (nome.Length < 2) return null;
        if (Apelidos.TryGetValue(nome, out var alias)) nome = alias;
        var lista = apps.ToArray();
        var exato = lista.FirstOrDefault(a => RoteadorIntencoes.Normalizar(a.Id) == nome ||
            RoteadorIntencoes.Normalizar(a.ApelidoVoz) == nome || RoteadorIntencoes.Normalizar(a.Nome) == nome);
        if (exato is not null) return exato;
        var candidatos = lista.Where(a => RoteadorIntencoes.Normalizar(a.Nome).Contains(nome)).ToArray();
        // Nomes ambíguos não escolhem um programa aleatoriamente.
        if (candidatos.Length == 1) return candidatos[0];
        if (!aproximar || nome.Length < 5) return null;
        var limite = nome.Length >= 8 ? 2 : 1;
        var proximos = lista.Where(a => !a.EhJogo && new[] { a.Nome, a.ApelidoVoz }.Any(n => Distancia(RoteadorIntencoes.Normalizar(n), nome) <= limite)).ToArray();
        return proximos.Length == 1 ? proximos[0] : null;
    }
    private static int Distancia(string a, string b)
    {
        if (Math.Abs(a.Length - b.Length) > 2) return 99;
        var linha = Enumerable.Range(0, b.Length + 1).ToArray();
        for (var i = 1; i <= a.Length; i++) { var anterior = linha[0]; linha[0] = i; for (var j = 1; j <= b.Length; j++) { var acima = linha[j]; linha[j] = Math.Min(Math.Min(linha[j] + 1, linha[j - 1] + 1), anterior + (a[i - 1] == b[j - 1] ? 0 : 1)); anterior = acima; } }
        return linha[b.Length];
    }
}
