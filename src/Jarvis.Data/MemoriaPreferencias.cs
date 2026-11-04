using System.Text.Json;
using Jarvis.Core;
namespace Jarvis.Data;
/// <summary>Somente preferências explícitas. Não extrai conteúdo de conversas ou e-mails.</summary>
public sealed class MemoriaPreferencias : IMemoriaPreferencias
{
    private readonly string _arquivo;
    private readonly object _gate = new();
    private sealed record Estado(bool Ativa, Dictionary<string, string> Valores);
    private Estado _estado = new(false, new());
    public MemoriaPreferencias(string? arquivo = null)
    {
        _arquivo = arquivo ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis", "memoria-preferencias.json");
        try { _estado = JsonSerializer.Deserialize<Estado>(File.ReadAllText(_arquivo)) ?? _estado; } catch { }
    }
    public bool Ativa { get { lock (_gate) return _estado.Ativa; } set { lock (_gate) { _estado = _estado with { Ativa = value }; Persistir(); } } }
    public IReadOnlyList<PreferenciaPessoal> Listar() { lock (_gate) return _estado.Valores.OrderBy(v => v.Key).Select(v => new PreferenciaPessoal(v.Key, v.Value)).ToArray(); }
    public void Salvar(string chave, string valor)
    {
        if (string.IsNullOrWhiteSpace(chave) || chave.Length > 80 || string.IsNullOrWhiteSpace(valor) || valor.Length > 500) throw new ArgumentException("Use um nome de até 80 caracteres e um valor de até 500.");
        lock (_gate) { if (!_estado.Ativa) throw new InvalidOperationException("Ative a memória de preferências primeiro."); _estado.Valores[chave.Trim()] = valor.Trim(); Persistir(); }
    }
    public void Apagar(string chave) { lock (_gate) { _estado.Valores.Remove(chave); Persistir(); } }
    public void Limpar() { lock (_gate) { _estado.Valores.Clear(); Persistir(); } }
    private void Persistir()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_arquivo))!);
        File.WriteAllText(_arquivo + ".tmp", JsonSerializer.Serialize(_estado)); File.Move(_arquivo + ".tmp", _arquivo, true);
    }
}
