// Clima via Open-Meteo (gratuito, sem chave). Portado de Jarvis_test.
using System.Globalization;
using System.Text.Json;

namespace Jarvis.Info;

public sealed record PrevisaoTempo(string Cidade, string Descricao, double Maxima, double Minima, int ChanceChuva, string TextoFalado);

public static class WeatherService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private static readonly Dictionary<string, (double Lat, double Lon, string Nome)> GeoCache = new();

    private static readonly (int Codigo, string Descricao)[] Descricoes = new[]
    {
        (0, "céu limpo"), (1, "quase limpo"), (2, "parcialmente nublado"), (3, "nublado"),
    };

    public static async Task<PrevisaoTempo?> ObterAsync(string cidade, CancellationToken ct = default)
    {
        try
        {
            var geo = await ResolverGeoAsync(cidade, ct);
            if (geo is null) return null;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(15));
            var url = $"https://api.open-meteo.com/v1/forecast?latitude={geo.Value.Lat.ToString(CultureInfo.InvariantCulture)}&longitude={geo.Value.Lon.ToString(CultureInfo.InvariantCulture)}&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max&timezone=auto&forecast_days=1";
            var json = await Http.GetStringAsync(url, cts.Token);
            using var doc = JsonDocument.Parse(json);
            var daily = doc.RootElement.GetProperty("daily");
            var maxT = daily.GetProperty("temperature_2m_max")[0].GetDouble();
            var minT = daily.GetProperty("temperature_2m_min")[0].GetDouble();
            var code = daily.GetProperty("weather_code")[0].GetInt32();
            var chuva = 0;
            if (daily.TryGetProperty("precipitation_probability_max", out var pp) && pp.GetArrayLength() > 0)
                try { chuva = pp[0].GetInt32(); } catch { }
            var desc = DescricaoCodigo(code);
            var extra = chuva >= 40 ? $" Chance de chuva de {chuva}%." : "";
            var falado = $"Em {cidade}: {desc}, máxima de {maxT:F0} graus e mínima de {minT:F0} graus.{extra}";
            return new PrevisaoTempo(cidade, desc, maxT, minT, chuva, falado);
        }
        catch { return null; }
    }

    private static string DescricaoCodigo(int code) => code switch
    {
        0 => "céu limpo",
        1 => "quase limpo",
        2 => "parcialmente nublado",
        3 => "nublado",
        45 or 48 => "neblina",
        51 or 53 or 55 => "garoa",
        61 or 63 or 65 => "chuva",
        71 or 73 or 75 or 77 => "neve",
        80 or 81 or 82 => "pancadas de chuva",
        95 or 96 or 99 => "tempestade",
        _ => "tempo instável",
    };

    private static async Task<(double Lat, double Lon, string Nome)?> ResolverGeoAsync(string cidade, CancellationToken ct)
    {
        try
        {
            var chave = cidade.ToLowerInvariant();
            if (GeoCache.TryGetValue(chave, out var hit)) return hit;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(15));
            var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(cidade)}&count=1&language=pt";
            var json = await Http.GetStringAsync(url, cts.Token);
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
                return null;
            var g = results[0];
            var geo = (g.GetProperty("latitude").GetDouble(), g.GetProperty("longitude").GetDouble(),
                g.TryGetProperty("name", out var n) ? n.GetString() ?? cidade : cidade);
            GeoCache[chave] = geo;
            return geo;
        }
        catch { return null; }
    }
}
