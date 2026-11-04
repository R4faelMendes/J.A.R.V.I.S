using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
namespace Jarvis.Core;
public sealed record ComandoRemotoVoz(string Nome, string? Argumento = null, bool Risco = false)
{
    public static string Normalizar(string texto)
    {
        var sb = new StringBuilder();
        foreach (var c in texto.ToLowerInvariant().Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(char.IsPunctuation(c) ? ' ' : c);
        return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
    }
    public static ComandoRemotoVoz? Traduzir(string texto)
    {
        var t = Normalizar(texto); var espaco = t.IndexOf(' ');
        if (espaco > 0 && NomeJarvis(t[..espaco])) t = t[(espaco + 1)..];
        t = Regex.Replace(t, @"^(?:voce pode|pode|por favor)\s+", "");
        t = Regex.Replace(t, @"(?:\s+(?:por favor|pra mim|para mim|agora))+$", "");
        t = Regex.Replace(t, @"\s+no (?:pc|computador|windows)$", "");
        var fixedCommand = t switch
        {
            "pausar" or "pause" or "pausar musica" or "pause a musica" or "pausar a musica" => "midia_pausar",
            "continuar musica" or "continuar a musica" or "tocar musica" => "midia_continuar",
            "proxima musica" or "proxima faixa" => "midia_proxima",
            "musica anterior" or "faixa anterior" => "midia_anterior",
            "bloquear pc" or "bloquear computador" => "bloquear",
            "suspender pc" or "suspender computador" => "suspender",
            "reiniciar pc" or "reiniciar computador" => "reiniciar",
            "ler desempenho" or "desempenho" or "qual o desempenho" or "como esta o desempenho" => "ler_desempenho",
            _ => null
        };
        if (fixedCommand is not null) return new(fixedCommand, Risco: fixedCommand is "bloquear" or "suspender" or "reiniciar");
        var m = Regex.Match(t, @"^volume (\d{1,3})(?: por cento)?$");
        if (m.Success && int.Parse(m.Groups[1].Value) <= 100) return new("volume", m.Groups[1].Value);
        m = Regex.Match(t, @"^(?:(?:coloque|coloca|ajuste|defina) (?:o )?)?volume\s+(?:em |para )?(.+)$");
        if (m.Success && NumeroPortugues.Tentar(m.Groups[1].Value, out var volume)) return new("volume", volume.ToString(CultureInfo.InvariantCulture));
        m = Regex.Match(t, @"^(abrir|abra|abre|abri|abriu|fechar|feche|fecha) (?:o |a )?(.+)$");
        if (m.Success) return new(m.Groups[1].Value.StartsWith("abr") ? "abrir_app" : "fechar_app", m.Groups[2].Value, true);
        return null;
    }
    private static bool NomeJarvis(string token)
    {
        // Variante observada na transcrição local de áudio PT-BR. Continua exigindo comando fixo após o nome.
        if (token == "pervis") return true;
        const string nome = "jarvis";
        if (token.Length == nome.Length) return token.Zip(nome).Count(p => p.First != p.Second) <= 1;
        if (token.Length == nome.Length - 1) return Enumerable.Range(0, nome.Length).Any(i => nome.Remove(i, 1) == token);
        if (token.Length == nome.Length + 1) return Enumerable.Range(0, token.Length).Any(i => token.Remove(i, 1) == nome);
        return false;
    }
    public static AppInfo? EncontrarApp(IEnumerable<AppInfo> aplicativos, string alvo)
    {
        var apps = aplicativos.ToArray(); var texto = Normalizar(alvo);
        for (var tentativa = 0; tentativa < 4; tentativa++)
        {
            var termo = texto switch { "crome" or "crom" or "google crome" => "chrome", "espotifai" or "espotify" => "spotify", "calculador" => "calculadora", _ => texto };
            var encontrados = apps.Where(a => Normalizar(a.Id) == termo || Normalizar(a.Nome) == termo || Normalizar(a.ApelidoVoz) == termo).ToArray();
            if (encontrados.Length == 0 && termo.Length >= 3) encontrados = apps.Where(a => Normalizar(a.Nome).Contains(termo) || Normalizar(a.ApelidoVoz).Contains(termo)).ToArray();
            if (encontrados.Length == 1) return encontrados[0];
            if (encontrados.Length > 1) return null;
            var espaco = texto.IndexOf(' '); if (espaco <= 0 || texto[..espaco] is not ("o" or "a" or "e" or "um" or "uma" or "aplicativo" or "app" or "programa")) return null;
            texto = texto[(espaco + 1)..];
        }
        return null;
    }
}
