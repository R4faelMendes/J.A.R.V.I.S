// JARVIS.Data — permissões por tipo de ação. Risco sempre confirma por padrão.
namespace Jarvis.Data;

/// <summary>Tipos de ação controlados pela tela Segurança.</summary>
public enum TipoAcao { AbrirApp, FecharApp, ControlarJanela, ReiniciarPc, EncerrarProcesso }

/// <summary>Permissão: Sozinho (faz sem perguntar), Confirmar (padrão p/ risco) ou Negado.</summary>
public enum Permissao { Sozinho, Confirmar, Negado }

/// <summary>Guarda as permissões em arquivo local. A UI confirma via modal âmbar.</summary>
public sealed class Permissoes
{
    private static readonly HashSet<TipoAcao> Risco = new()
    {
        TipoAcao.FecharApp, TipoAcao.EncerrarProcesso, TipoAcao.ReiniciarPc,
    };

    private readonly Dictionary<TipoAcao, Permissao> _mapa = new();
    private readonly string _arquivo;

    public Permissoes()
    {
        var pasta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis");
        Directory.CreateDirectory(pasta);
        _arquivo = Path.Combine(pasta, "permissoes.json");
        foreach (TipoAcao t in Enum.GetValues<TipoAcao>())
            _mapa[t] = Risco.Contains(t) ? Permissao.Confirmar : Permissao.Sozinho;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(_arquivo));
            foreach (var p in doc.RootElement.EnumerateObject())
                if (Enum.TryParse<TipoAcao>(p.Name, out var t) && Enum.TryParse<Permissao>(p.Value.GetString(), out var v))
                    _mapa[t] = v;
        }
        catch { }
    }

    public Permissao Obter(TipoAcao tipo) => _mapa.TryGetValue(tipo, out var p) ? p : Permissao.Confirmar;

    public void Definir(TipoAcao tipo, Permissao valor)
    {
        _mapa[tipo] = valor;
        File.WriteAllText(_arquivo, System.Text.Json.JsonSerializer.Serialize(
            _mapa.ToDictionary(k => k.Key.ToString(), v => v.Value.ToString())));
    }

    /// <summary>true se pode executar direto; false se precisa confirmar (ou negado).</summary>
    public bool PodeExecutar(TipoAcao tipo, out bool precisaConfirmar)
    {
        var p = Obter(tipo);
        precisaConfirmar = p == Permissao.Confirmar;
        return p != Permissao.Negado;
    }
}
