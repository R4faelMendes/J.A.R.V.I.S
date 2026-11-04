using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Jarvis.Ai;

public sealed record Intencao(string Tipo, string? Resposta = null, string? Acao = null, string? Alvo = null);

public static class RoteadorIntencoes
{
    public static string Normalizar(string texto)
    {
        var sb = new StringBuilder();
        foreach (var c in texto.ToLowerInvariant().Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(char.IsPunctuation(c) ? ' ' : c);
        return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
    }

    public static Intencao? Rotear(string texto)
    {
        var t = Normalizar(texto);
        t = Regex.Replace(t, @"^(?:jarvis\s+)?(?:por favor\s+)?", "");
        t = Regex.Replace(t, @"(?:\s+(?:por favor|por gentileza|pra mim|para mim|agora))+$", "");
        t = Regex.Replace(t, @"^(?:voce pode|pode|eu quero que voce)\s+", "");
        // Variação observada no Whisper ao ouvir "abrir a calculadora".
        t = Regex.Replace(t, @"^abri\s+la\s+", "abrir ");
        if (t.Length == 0) return new("vazio", "Não ouvi nada. Pode repetir?");
        if (Regex.IsMatch(t, @"^(confirmar|confirmo|pode fechar)$")) return new("confirmar", Acao: "confirm");
        if (Regex.IsMatch(t, @"^(cancelar|cancela|nao)$")) return new("cancelar", Acao: "cancel");
        if (Regex.IsMatch(t, @"^(pare|parar|para tudo|chega|silencio)$")) return new("parar", Acao: "stop");
        if (Regex.IsMatch(t, @"^(leia|ler|le|mostre|mostrar|resuma|resumir|quais sao|ver)\b.*\b(e mails|emails|email|mensagens|caixa de entrada)\b") || t == "meus emails")
            return new("emails", Acao: "read_inbox");
        if (Regex.IsMatch(t, @"^(pausa|pause|pausar|pare a musica|parar a musica)(\b.*)?$")) return new("midia", Acao: "media_pause");
        if (Regex.IsMatch(t, @"^(continua|continue|continuar|retoma|retome|retomar)(\b.*)?$|^(toca|toque|tocar|reproduzir) (a musica|musica|o spotify|spotify)$")) return new("midia", Acao: "media_play");
        if (Regex.IsMatch(t, @"^(proxima( musica| faixa| cancao)?|pula( a musica| essa| essa musica)?|pular( a musica)?|troca( a musica)?|trocar( de musica)?)( no spotify)?$")) return new("midia", Acao: "media_next");
        if (Regex.IsMatch(t, @"^(anterior|musica anterior|faixa anterior|volta( a musica| uma musica)?|voltar( a musica| uma musica)?)( no spotify)?$")) return new("midia", Acao: "media_previous");
        if (Regex.IsMatch(t, @"^(que|qual) musica (e essa|esta tocando)$")) return new("midia", Acao: "media_status");
        var m = Regex.Match(t, @"^(?:(?:coloque|coloca|colocar|ajuste|defina) (?:o )?)?volume\s+(?:para\s+|em\s+)?(.+)$");
        if (m.Success && NumeroFalado.Tentar(m.Groups[1].Value, out var volume)) return new("volume", Acao: "set_volume", Alvo: volume.ToString(System.Globalization.CultureInfo.InvariantCulture));
        m = Regex.Match(t, @"^(abre|abra|abrir|abrira|inicia|inicie|iniciar|execute|executar)\s+(?:o\s+|a\s+)?(.+)$");
        if (m.Success) return new("abrir_app", Acao: "open_app", Alvo: m.Groups[2].Value);
        m = Regex.Match(t, @"^(fecha|feche|fechar)\s+(?:o\s+|a\s+)?(.+)$");
        if (m.Success) return new("fechar_app", Acao: "close_app", Alvo: m.Groups[2].Value);
        if (Regex.IsMatch(t, @"que horas|qual.*hora|horas sao")) return new("hora", $"São {DateTime.Now.Hour} horas e {DateTime.Now.Minute} minutos.");
        if (Regex.IsMatch(t, @"que dia|data de hoje|dia de hoje")) return new("data", DateTime.Today.ToString("'Hoje é' dddd, d 'de' MMMM 'de' yyyy.", CultureInfo.GetCultureInfo("pt-BR")));
        if (Regex.IsMatch(t, @"como esta.*(pc|computador)|uso de (cpu|mem)")) return new("desempenho", Acao: "perf_status");
        if (Regex.IsMatch(t, @"^(oi|ola|bom dia|boa tarde|boa noite)\b"))
            return new("saudacao", (DateTime.Now.Hour < 12 ? "Bom dia" : DateTime.Now.Hour < 18 ? "Boa tarde" : "Boa noite") + ". Em que posso ajudar?");
        return null;
    }
}
