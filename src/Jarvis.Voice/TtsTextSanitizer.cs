// Sanitiza texto para TTS falado (pt-BR): datas, horas, %, R$, links, emojis. Portado de Jarvis_test.
using System.Text.RegularExpressions;

namespace Jarvis.Voice;

public static class TtsTextSanitizer
{
    private static readonly string[] Meses =
        { "", "janeiro", "fevereiro", "março", "abril", "maio", "junho",
          "julho", "agosto", "setembro", "outubro", "novembro", "dezembro" };

    public static string Sanitize(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        // remove markdown e símbolos que o TTS soletraria
        var s = Regex.Replace(input, @"[*_~`#>|]", "");
        // emojis e símbolos diversos
        s = Regex.Replace(s, @"\p{Cs}", "");
        s = Regex.Replace(s, @"[☀-➿⬀-⯿️-️]", "");
        // URLs viram "link"
        s = Regex.Replace(s, @"https?://\S+", "link");
        // horas: 09:00 -> 9 horas ; 14:30 -> 14 e 30
        s = Regex.Replace(s, @"\b0?(\d{1,2}):00\b", "$1 horas");
        s = Regex.Replace(s, @"\b0?(\d{1,2}):(\d{2})\b", "$1 e $2");
        // datas: 16/07 -> 16 de julho
        s = Regex.Replace(s, @"\b(\d{1,2})/(\d{1,2})(?:/\d{2,4})?\b", m =>
        {
            if (int.TryParse(m.Groups[2].Value, out var mes) && mes >= 1 && mes <= 12)
                return m.Groups[1].Value.TrimStart('0') + " de " + Meses[mes];
            return m.Value;
        });
        s = Regex.Replace(s, @"(\d+)%", "$1 por cento");
        s = Regex.Replace(s, @"R\$\s*(\d+)", "$1 reais");
        return Regex.Replace(s, @"\s+", " ").Trim();
    }
}
