namespace Jarvis.Core;
public static class NumeroPortugues
{
    public static bool Tentar(string texto, out int valor)
    {
        texto = ComandoRemotoVoz.Normalizar(texto).Replace(" por cento", "").Replace(" porcento", "").Trim();
        if (int.TryParse(texto, out valor)) return valor is >= 0 and <= 100;
        var palavras = new Dictionary<string, int> { ["zero"] = 0, ["um"] = 1, ["uma"] = 1, ["dois"] = 2, ["duas"] = 2, ["tres"] = 3, ["quatro"] = 4, ["cinco"] = 5, ["seis"] = 6, ["sete"] = 7, ["oito"] = 8, ["nove"] = 9, ["dez"] = 10, ["onze"] = 11, ["doze"] = 12, ["treze"] = 13, ["quatorze"] = 14, ["catorze"] = 14, ["quinze"] = 15, ["dezesseis"] = 16, ["dezessete"] = 17, ["dezoito"] = 18, ["dezenove"] = 19, ["vinte"] = 20, ["trinta"] = 30, ["quarenta"] = 40, ["cinquenta"] = 50, ["sessenta"] = 60, ["setenta"] = 70, ["oitenta"] = 80, ["noventa"] = 90, ["cem"] = 100 };
        if (palavras.TryGetValue(texto, out valor)) return true;
        var parts = texto.Split(" e ", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2 && palavras.TryGetValue(parts[0], out var dezena) && palavras.TryGetValue(parts[1], out var unidade) && dezena is >= 20 and <= 90 && unidade is >= 1 and <= 9) { valor = dezena + unidade; return true; }
        valor = 0; return false;
    }
}

