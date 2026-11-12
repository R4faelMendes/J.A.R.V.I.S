using System.Text.Json;
using System.Text.RegularExpressions;
using Jarvis.Core;
namespace Jarvis.Ai;

/// <summary>Somente respostas textuais. Nenhuma saída de provedor é enviada ao executor de ações.</summary>
public sealed class ConversaIaService : IConversaIa
{
    private readonly string _pasta;
    private readonly Func<PreferenciasIa, IReadOnlyList<IChatProvider>>? _factory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly object _config = new();
    private CancellationTokenSource _consentimento = new();
    private PreferenciasIa _preferencias;
    public PreferenciasIa Preferencias { get { lock (_config) return _preferencias; } }
    public ConversaIaService(string? pasta = null, Func<PreferenciasIa, IReadOnlyList<IChatProvider>>? factory = null)
    {
        _pasta = pasta ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis");
        Directory.CreateDirectory(_pasta); _factory = factory;
        try { _preferencias = JsonSerializer.Deserialize<PreferenciasIa>(File.ReadAllText(Path.Combine(_pasta, "conversa-ia.json"))) ?? new(); }
        catch (IOException) { _preferencias = new(); } catch (JsonException) { _preferencias = new(); }
    }
    private static void Salvar<T>(string arquivo, T valor)
    {
        var temp = arquivo + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temp, JsonSerializer.Serialize(valor)); File.Move(temp, arquivo, true); }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    public void Configurar(PreferenciasIa preferencias, string provedor, string? chave, bool removerChave = false)
    {
        if (provedor is not ("gemini" or "groq" or "openrouter")) throw new ArgumentException("Provedor inválido.");
        foreach (var modelo in new[] { preferencias.Gemini, preferencias.Groq, preferencias.OpenRouter })
            if (!Regex.IsMatch(modelo ?? "", "^[a-zA-Z0-9][a-zA-Z0-9._/:-]{0,159}$")) throw new ArgumentException("Informe identificadores válidos dos modelos.");
        lock (_config) {
            if (removerChave || !string.IsNullOrWhiteSpace(chave)) {
                var cofre = new CofreChaves(); var chaves = cofre.Ler();
                if (removerChave) chaves.Remove(provedor); else chaves[provedor] = chave!.Trim();
                cofre.Guardar(chaves);
            }
            Salvar(Path.Combine(_pasta, "conversa-ia.json"), preferencias);
            _preferencias = preferencias;
            if (!preferencias.Ativa) { _consentimento.Cancel(); _consentimento.Dispose(); _consentimento = new(); }
        }
    }
    private IReadOnlyList<IChatProvider> Provedores(PreferenciasIa p)
    {
        if (_factory is not null) return _factory(p);
        var keys = new CofreChaves().Ler(); var lista = new List<IChatProvider>();
        if (keys.TryGetValue("groq", out var groq) && !string.IsNullOrWhiteSpace(groq)) lista.Add(new GroqProvider { Chave = groq, Modelo = p.Groq });
        if (keys.TryGetValue("gemini", out var gemini) && !string.IsNullOrWhiteSpace(gemini)) lista.Add(new GeminiProvider { Chave = gemini, Modelo = p.Gemini });
        if (keys.TryGetValue("openrouter", out var router) && !string.IsNullOrWhiteSpace(router)) lista.Add(new OpenRouterProvider { Chave = router, Modelo = p.OpenRouter });
        return lista;
    }
    public IReadOnlyList<string> ProvedoresConfigurados => Provedores(Preferencias).Select(p => p.Nome).ToArray();
    private sealed record Consumo(string Dia, int Tentativas);
    public int UsadasHoje {
        get {
            try { var c = JsonSerializer.Deserialize<Consumo>(File.ReadAllText(Path.Combine(_pasta, "consumo-ia.json"))); return c?.Dia == DateTime.Today.ToString("yyyy-MM-dd") ? Math.Clamp(c.Tentativas, 0, 20) : 0; }
            catch (IOException) { return 0; } catch (JsonException) { return 0; }
        }
    }
    public async Task<RespostaIa> PerguntarAsync(string pergunta, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(pergunta) || pergunta.Length > 4000) throw new ArgumentException("Pergunta inválida.");
        await _gate.WaitAsync(ct);
        try {
            PreferenciasIa preferencias; CancellationToken consentimento;
            lock (_config) { preferencias = _preferencias; consentimento = _consentimento.Token; }
            if (!preferencias.Ativa) return new(AssistenteLocal.Responder(pergunta), "Local", false);
            using var ligado = CancellationTokenSource.CreateLinkedTokenSource(ct, consentimento);
            var provedores = Provedores(preferencias);
            if (provedores.Count == 0) return new("Configure uma chave de IA em Configurações para perguntas gerais. " + AssistenteLocal.Responder(pergunta), "Local", false);
            foreach (var provedor in provedores) {
                ligado.Token.ThrowIfCancellationRequested();
                var usadas = UsadasHoje;
                if (usadas >= 20) return new("Limite local de 20 tentativas de IA hoje. Os comandos locais continuam disponíveis.", "Local", false);
                Salvar(Path.Combine(_pasta, "consumo-ia.json"), new Consumo(DateTime.Today.ToString("yyyy-MM-dd"), usadas + 1));
                try {
                    var resposta = await provedor.PerguntarAsync("Responda em português. Você fornece apenas texto e não pode executar ações no computador. Não afirme ter aberto, fechado ou alterado algo. Pergunta do usuário: " + pergunta, ligado.Token);
                    ligado.Token.ThrowIfCancellationRequested();
                    if (!string.IsNullOrWhiteSpace(resposta)) return new(resposta.Trim(), provedor.Nome, true);
                }
                catch (OperationCanceledException) when (ligado.IsCancellationRequested) { throw; }
                catch (HttpRequestException) { }
                catch (JsonException) { }
                catch (InvalidOperationException) { }
                catch (OperationCanceledException) { } // Timeout do provedor; tenta o seguinte.
            }
            return new("Os provedores configurados não responderam. Confira conexão, chave, modelo e limites da conta. Os comandos locais continuam disponíveis.", "Local", false);
        } finally { _gate.Release(); }
    }
}
