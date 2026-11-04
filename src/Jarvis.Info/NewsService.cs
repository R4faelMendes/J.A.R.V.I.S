// Notícias via RSS do Google News (gratuito, sem chave). Portado de Jarvis_test.
using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;

namespace Jarvis.Info;

public sealed record Noticia(string Titulo, string Link, string Fonte, string Categoria, DateTime PublicadoEm);

public static class NewsService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };
    private static string PastaCache
    {
        get
        {
            var p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis");
            Directory.CreateDirectory(p);
            return Path.Combine(p, "news_cache.json");
        }
    }

    public static async Task<IReadOnlyList<Noticia>> BuscarTopicoAsync(string categoria, int quantidade = 5, CancellationToken ct = default)
    {
        var lista = await BuscarRssAsync(categoria, quantidade, comFiltroDia: true, ct);
        if (lista.Count < quantidade)
            lista = await BuscarRssAsync(categoria, quantidade, comFiltroDia: false, ct);
        var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return lista.Where(n => vistos.Add(n.Titulo)).Take(quantidade).ToList();
    }

    private static async Task<List<Noticia>> BuscarRssAsync(string categoria, int quantidade, bool comFiltroDia, CancellationToken ct)
    {
        var lista = new List<Noticia>();
        try
        {
            var q = Uri.EscapeDataString(categoria + (comFiltroDia ? " when:1d" : ""));
            var url = $"https://news.google.com/rss/search?q={q}&hl=pt-BR&gl=BR&ceid=BR:pt-419";
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(15));
            var xml = await Http.GetStringAsync(url, cts.Token);
            var doc = XDocument.Parse(xml);
            foreach (var item in doc.Descendants("item"))
            {
                var bruto = item.Element("title")?.Value ?? "";
                var fonte = (item.Element("source")?.Value ?? "Google News").Trim();
                var link = item.Element("link")?.Value ?? "";
                var pub = DateTime.Now;
                var pubStr = item.Element("pubDate")?.Value ?? "";
                if (!string.IsNullOrEmpty(pubStr) &&
                    DateTimeOffset.TryParse(pubStr, System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var dto))
                    pub = dto.LocalDateTime;
                var titulo = LimparTitulo(bruto, fonte);
                if (!string.IsNullOrEmpty(titulo))
                    lista.Add(new Noticia(titulo, link, fonte, categoria, pub));
                if (lista.Count >= quantidade) break;
            }
        }
        catch { }
        return lista;
    }

    public static string LimparTitulo(string bruto, string fonte)
    {
        if (string.IsNullOrWhiteSpace(bruto)) return string.Empty;
        var t = bruto.Trim();
        if (!string.IsNullOrWhiteSpace(fonte))
        {
            var sufixo = " - " + fonte.Trim();
            if (t.EndsWith(sufixo, StringComparison.OrdinalIgnoreCase))
                t = t[..^sufixo.Length].Trim();
        }
        return t;
    }

    public static void AbrirLink(string url)
    {
        try
        {
            var u = new Uri(url);
            if (u.Scheme != "http" && u.Scheme != "https") return;
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch { }
    }
}
