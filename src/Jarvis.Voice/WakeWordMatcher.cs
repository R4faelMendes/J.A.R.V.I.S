// Wake word tolerante: minúsculas, sem acento, distância de edição ≤ 1 por palavra. Portado de Jarvis_test.
using System.Globalization;
using System.Text;

namespace Jarvis.Voice;

public static class WakeWordMatcher
{
    public static bool ContainsWakeWord(string heard, string wakeWord)
    {
        heard = NormalizarApelidos(heard, wakeWord);
        if (string.IsNullOrWhiteSpace(heard) || string.IsNullOrWhiteSpace(wakeWord)) return false;
        var h = Normalize(heard).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var w = Normalize(wakeWord).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (w.Length == 0 || h.Length < w.Length) return false;
        for (var i = 0; i <= h.Length - w.Length; i++)
        {
            var ok = true;
            for (var j = 0; j < w.Length; j++)
            {
                var a = h[i + j];
                var b = w[j];
                if (a == b) continue;
                if (Math.Abs(a.Length - b.Length) > 1) { ok = false; break; }
                if (EditDistance(a, b) > 1) { ok = false; break; }
            }
            if (ok) return true;
        }
        return false;
    }

    // Remove a wake word do início da frase (wake + comando na mesma frase).
    public static string StripWakeWord(string heard, string wakeWord)
    {
        heard = NormalizarApelidos(heard, wakeWord);
        var h = heard.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        var w = wakeWord.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (h.Count < w.Length) return heard;
        var normH = h.Select(Normalize).ToList();
        var normW = w.Select(Normalize).ToList();
        for (var i = 0; i <= normH.Count - normW.Count; i++)
        {
            var ok = true;
            for (var j = 0; j < normW.Count; j++)
                if (normH[i + j] != normW[j] && EditDistance(normH[i + j], normW[j]) > 1) { ok = false; break; }
            if (ok) return string.Join(' ', h.Skip(i + normW.Count));
        }
        return heard;
    }

    public static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var form = s.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(form.Length);
        foreach (var c in form)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(char.IsPunctuation(c) ? ' ' : c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
    private static string NormalizarApelidos(string heard, string wake)
    {
        if (!Normalize(wake).Equals("jarvis")) return heard;
        return System.Text.RegularExpressions.Regex.Replace(Normalize(heard), @"^\s*(?:(?:ei|oi|ola)\s+)?(?:jarvis|jarbas|jarves|javis|ja vis|eerviz|erviz|gervis|jerviz|jarviz|pervis|quervis)\b", "jarvis");
    }
    public static bool AtivacaoNoInicio(string heard, string wakeWord)
    {
        var text = NormalizarApelidos(heard, wakeWord).Trim();
        text = System.Text.RegularExpressions.Regex.Replace(text, @"^(?:ei|oi|ola)\s+", "");
        var count = Normalize(wakeWord).Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        return count > 0 && ContainsWakeWord(string.Join(' ', text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(count)), wakeWord);
    }

    public static int EditDistance(string a, string b)
    {
        if (a == b) return 0;
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;
        var prev = new int[b.Length + 1];
        var cur = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) prev[j] = j;
        for (var i = 1; i <= a.Length; i++)
        {
            cur[0] = i;
            for (var j = 1; j <= b.Length; j++)
                cur[j] = Math.Min(Math.Min(cur[j - 1] + 1, prev[j] + 1),
                                  prev[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            (prev, cur) = (cur, prev);
        }
        return prev[b.Length];
    }
}
