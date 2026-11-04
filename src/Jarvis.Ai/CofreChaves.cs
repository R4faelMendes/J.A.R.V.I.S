// JARVIS.Ai — chaves de IA guardadas com DPAPI (nunca em texto puro, nunca no repo).
using System.Security.Cryptography;
using System.Text.Json;

namespace Jarvis.Ai;

/// <summary>Cofre local das chaves dos provedores (gemini, groq, openrouter).</summary>
public sealed class CofreChaves
{
    private readonly string _arquivo;
    public CofreChaves()
    {
        var pasta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis");
        Directory.CreateDirectory(pasta);
        _arquivo = Path.Combine(pasta, "ia.dat");
    }

    public Dictionary<string, string> Ler()
    {
        try
        {
            var claro = ProtectedData.Unprotect(File.ReadAllBytes(_arquivo), null, DataProtectionScope.CurrentUser);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(claro) ?? new();
        }
        catch { return new(); }
    }

    public void Guardar(Dictionary<string, string> chaves)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(chaves);
        File.WriteAllBytes(_arquivo, ProtectedData.Protect(json, null, DataProtectionScope.CurrentUser));
    }
}

/// <summary>Extensões do ChatService: carrega chaves do cofre nos provedores.</summary>
public static class ChatServiceExt
{
    public static ChatService ComChavesDoCofre(this ChatService chat)
    {
        var chaves = new CofreChaves().Ler();
        if (chaves.TryGetValue("gemini", out var g) && !string.IsNullOrWhiteSpace(g))
            chat.Adicionar(new GeminiProvider { Chave = g });
        if (chaves.TryGetValue("groq", out var q) && !string.IsNullOrWhiteSpace(q))
            chat.Adicionar(new GroqProvider { Chave = q });
        if (chaves.TryGetValue("openrouter", out var o) && !string.IsNullOrWhiteSpace(o))
            chat.Adicionar(new OpenRouterProvider { Chave = o });
        return chat;
    }
}
