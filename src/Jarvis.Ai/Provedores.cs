// JARVIS.Ai — cadeia de provedores gratuitos com fallback + cota diária.
using System.Net.Http.Json;
using System.Text.Json;
using Jarvis.Core;

namespace Jarvis.Ai;

/// <summary>Base dos provedores: chave do usuário, sem chave embutida.</summary>
public abstract class ProvedorBase : IChatProvider
{
    protected static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };
    public abstract string Nome { get; }
    public string? Chave { get; set; }
    public abstract Task<string> PerguntarAsync(string texto, CancellationToken ct = default);
}

/// <summary>Google Gemini (AI Studio) — camada gratuita generosa.</summary>
public sealed class GeminiProvider : ProvedorBase
{
    public override string Nome => "Gemini";
    public string Modelo { get; set; } = "gemini-3.5-flash-lite";
    public override async Task<string> PerguntarAsync(string texto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(Chave)) throw new InvalidOperationException("Sem chave.");
        var corpo = new { contents = new[] { new { parts = new[] { new { text = texto } } } } };
        using var req = new HttpRequestMessage(HttpMethod.Post, $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(Modelo)}:generateContent");
        req.Headers.Add("x-goog-api-key", Chave);
        req.Content = JsonContent.Create(corpo);
        using var r = await Http.SendAsync(req, ct);
        r.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync(ct));
        return doc.RootElement.GetProperty("candidates")[0]
            .GetProperty("content").GetProperty("parts")[0]
            .GetProperty("text").GetString() ?? "";
    }
}

/// <summary>Groq — inferência rápida com camada gratuita.</summary>
public sealed class GroqProvider : ProvedorBase
{
    public override string Nome => "Groq";
    public string Modelo { get; set; } = "openai/gpt-oss-20b";
    public override async Task<string> PerguntarAsync(string texto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(Chave)) throw new InvalidOperationException("Sem chave.");
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Chave);
        req.Content = JsonContent.Create(new
        {
            model = Modelo,
            messages = new[] { new { role = "user", content = texto } },
            max_tokens = 300,
        });
        using var r = await Http.SendAsync(req, ct);
        r.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync(ct));
        return doc.RootElement.GetProperty("choices")[0]
            .GetProperty("message").GetProperty("content").GetString() ?? "";
    }
}

/// <summary>OpenRouter — agregador com modelos gratuitos.</summary>
public sealed class OpenRouterProvider : ProvedorBase
{
    public override string Nome => "OpenRouter";
    public string Modelo { get; set; } = "openrouter/free";
    public override async Task<string> PerguntarAsync(string texto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(Chave)) throw new InvalidOperationException("Sem chave.");
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Chave);
        req.Content = JsonContent.Create(new
        {
            model = Modelo,
            messages = new[] { new { role = "user", content = texto } },
            max_tokens = 300,
        });
        using var r = await Http.SendAsync(req, ct);
        r.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync(ct));
        return doc.RootElement.GetProperty("choices")[0]
            .GetProperty("message").GetProperty("content").GetString() ?? "";
    }
}

/// <summary>Cota diária em arquivo local (sem banco nesta fase).</summary>
public sealed class CotaDiaria
{
    private readonly string _arquivo;
    public CotaDiaria()
    {
        var pasta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis");
        Directory.CreateDirectory(pasta);
        _arquivo = Path.Combine(pasta, "cota.json");
    }

    public int UsadasHoje()
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(_arquivo));
            var hoje = DateTime.Today.ToString("yyyy-MM-dd");
            return doc.RootElement.TryGetProperty(hoje, out var v) ? v.GetInt32() : 0;
        }
        catch { return 0; }
    }

    public void Registrar()
    {
        var hoje = DateTime.Today.ToString("yyyy-MM-dd");
        int n;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(_arquivo));
            n = doc.RootElement.TryGetProperty(hoje, out var v) ? v.GetInt32() : 0;
        }
        catch { n = 0; }
        File.WriteAllText(_arquivo, $"{{\"{hoje}\": {n + 1}}}");
    }
}

/// <summary>Orquestra: roteador local primeiro, depois cadeia com fallback e erro amigável.</summary>
public sealed class ChatService
{
    private readonly List<ProvedorBase> _provedores = new();
    private readonly CotaDiaria _cota = new();
    public const string ErroCota = "A IA atingiu o limite gratuito de hoje. Volte amanhã ou troque o provedor em Configurações.";

    public void Adicionar(ProvedorBase p) => _provedores.Add(p);
    public int UsadasHoje => _cota.UsadasHoje();

    public async Task<string> ResponderAsync(string texto, CancellationToken ct = default)
    {
        var rota = RoteadorIntencoes.Rotear(texto);
        if (rota?.Resposta is not null) return rota.Resposta;
        if (_provedores.Count == 0)
            return AssistenteLocal.Responder(texto);
        foreach (var p in _provedores)
        {
            try
            {
                var r = await p.PerguntarAsync(texto, ct);
                _cota.Registrar();
                return r.Trim();
            }
            catch { } // tenta o próximo
        }
        return ErroCota;
    }
}
