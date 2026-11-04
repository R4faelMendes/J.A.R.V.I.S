using System.Globalization;
using System.Text.RegularExpressions;
namespace Jarvis.Ai;
public static class AssistenteLocal
{
    public static string Responder(string texto)
    {
        var t = RoteadorIntencoes.Normalizar(texto);
        if (t.Contains("meu nome")) return $"Seu nome é {Jarvis.Core.ConfiguracaoApp.NomeUsuario}.";
        if (t.Contains("obrigad")) return "Por nada. Estou por aqui.";
        if (t.Contains("seu nome") || t.Contains("quem e voce")) return "Sou o Jarvis, seu assistente local para controlar o computador.";
        if (t.Contains("internet") || t.Contains("chave") || t.Contains("offline"))
            return "Meus comandos de aplicativos, música, hora e cálculo funcionam sem chave de IA. E-mails precisam da conexão com o Google.";
        var calculo = Regex.Match(texto.ToLowerInvariant().Replace("quanto é", "").Replace("quanto e", "").Replace("calcule", "").Trim().TrimEnd('?'),
            @"^(-?\d+(?:[.,]\d+)?)\s*(mais|menos|vezes|dividido por|[+*/-])\s*(-?\d+(?:[.,]\d+)?)$");
        if (calculo.Success)
        {
            var a = double.Parse(calculo.Groups[1].Value.Replace(',', '.'), CultureInfo.InvariantCulture);
            var b = double.Parse(calculo.Groups[3].Value.Replace(',', '.'), CultureInfo.InvariantCulture);
            var op = calculo.Groups[2].Value;
            if (b == 0 && op is "/" or "dividido por") return "Não é possível dividir por zero.";
            var r = op switch { "+" or "mais" => a + b, "-" or "menos" => a - b, "*" or "vezes" => a * b, _ => a / b };
            return $"O resultado é {r.ToString("G10", CultureInfo.GetCultureInfo("pt-BR"))}.";
        }
        return "Posso abrir e fechar aplicativos, controlar o Spotify, ler seus e-mails, dizer a hora e fazer cálculos simples. Experimente: abra a calculadora. Perguntas gerais ainda não fazem parte do meu modo local.";
    }
}
