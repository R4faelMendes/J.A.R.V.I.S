// JARVIS.Sistema — abas do Chrome/Edge via DevTools Protocol (sem extensão).
using System.Net.Http.Json;
using System.Text.Json;
using Jarvis.Core;

namespace Jarvis.Sistema;

/// <summary>Conversa com o navegador pela porta de depuração local.</summary>
public sealed class BrowserController : IBrowserController
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(5) };
    private const int Porta = 9222;

    private static async Task<JsonElement> AbasAsync()
    {
        using var r = await Http.GetAsync($"http://127.0.0.1:{Porta}/json/list");
        r.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    public IReadOnlyList<AbaInfo> ListarAbas()
    {
        try
        {
            return AbasAsync().GetAwaiter().GetResult().EnumerateArray()
                .Where(a => a.GetProperty("type").GetString() == "page")
                .Select(a => new AbaInfo(
                    a.GetProperty("id").GetString()!,
                    a.GetProperty("title").GetString() ?? "",
                    a.GetProperty("url").GetString() ?? ""))
                .ToList();
        }
        catch
        {
            throw new InvalidOperationException(
                "Navegador sem depuração. Abra o Chrome/Edge com --remote-debugging-port=9222.");
        }
    }

    public void AbrirEndereco(string url)
    {
        if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) url = "https://" + url;
        try
        {
            Http.GetAsync($"http://127.0.0.1:{Porta}/json/new?{Uri.EscapeDataString(url)}")
                .GetAwaiter().GetResult().EnsureSuccessStatusCode();
        }
        catch { throw new InvalidOperationException("Navegador sem depuração (porta 9222)."); }
    }

    public void IrParaAba(string id)
    {
        try
        {
            Http.GetAsync($"http://127.0.0.1:{Porta}/json/activate/{id}")
                .GetAwaiter().GetResult().EnsureSuccessStatusCode();
        }
        catch { throw new InvalidOperationException("Não achei a aba."); }
    }

    public void FecharAba(string id)
    {
        try
        {
            Http.GetAsync($"http://127.0.0.1:{Porta}/json/close/{id}")
                .GetAwaiter().GetResult().EnsureSuccessStatusCode();
        }
        catch { throw new InvalidOperationException("Não achei a aba."); }
    }
}
