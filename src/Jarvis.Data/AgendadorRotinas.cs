using System.Text.Json;
using Jarvis.Core;
using Microsoft.Data.Sqlite;
namespace Jarvis.Data;

public sealed class AgendadorRotinas : IAgendadorRotinas
{
    private readonly ICatalogoRotinas _catalogo;
    private readonly IRotinaService _executor;
    private readonly IHistoricoComandos _historico;
    private readonly string _arquivo;
    private readonly object _vidaGate = new();
    private CancellationTokenSource? _vida;
    private Timer? _timer;
    private int _executando;
    private string _ultimo = "Nenhum agendamento executado nesta sessão.";
    public string UltimoResultado => Volatile.Read(ref _ultimo);
    public AgendadorRotinas(ICatalogoRotinas catalogo, IRotinaService executor, IHistoricoComandos historico, string? arquivo = null)
    {
        _catalogo = catalogo; _executor = executor; _historico = historico;
        _arquivo = arquivo ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis", "jarvis.db");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_arquivo))!);
        using var db = Abrir(); using var cmd = db.CreateCommand();
        cmd.CommandText = "CREATE TABLE IF NOT EXISTS agendamentos (rotina INTEGER PRIMARY KEY, hora TEXT NOT NULL, dias TEXT NOT NULL, ativo INTEGER NOT NULL, ultima TEXT)"; cmd.ExecuteNonQuery();
    }
    private SqliteConnection Abrir() { var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _arquivo, DefaultTimeout = 5 }.ToString()); db.Open(); return db; }
    public IReadOnlyList<AgendamentoRotina> Listar()
    {
        using var db = Abrir(); using var cmd = db.CreateCommand(); cmd.CommandText = "SELECT rotina,hora,dias,ativo,ultima FROM agendamentos ORDER BY hora";
        using var r = cmd.ExecuteReader(); var itens = new List<AgendamentoRotina>();
        while (r.Read()) itens.Add(new(r.GetInt64(0), r.GetString(1), JsonSerializer.Deserialize<DayOfWeek[]>(r.GetString(2)) ?? [], r.GetInt64(3) == 1, r.IsDBNull(4) ? null : r.GetString(4)));
        return itens;
    }
    public void Salvar(AgendamentoRotina item)
    {
        RegraAgendamento.Validar(item);
        if (!_catalogo.Listar().Any(r => r.Id == item.RotinaId)) throw new ArgumentException("Rotina não encontrada.");
        using var db = Abrir(); using var cmd = db.CreateCommand();
        cmd.CommandText = "INSERT INTO agendamentos(rotina,hora,dias,ativo,ultima) VALUES($i,$h,$d,$a,NULL) ON CONFLICT(rotina) DO UPDATE SET hora=$h,dias=$d,ativo=$a";
        cmd.Parameters.AddWithValue("$i", item.RotinaId); cmd.Parameters.AddWithValue("$h", item.Hora); cmd.Parameters.AddWithValue("$d", JsonSerializer.Serialize(item.Dias)); cmd.Parameters.AddWithValue("$a", item.Ativo ? 1 : 0); cmd.ExecuteNonQuery();
    }
    public void Remover(long id) { using var db = Abrir(); using var cmd = db.CreateCommand(); cmd.CommandText = "DELETE FROM agendamentos WHERE rotina=$i"; cmd.Parameters.AddWithValue("$i", id); cmd.ExecuteNonQuery(); }
    private bool Reservar(AgendamentoRotina item, string chave)
    {
        using var db = Abrir(); using var cmd = db.CreateCommand();
        cmd.CommandText = "UPDATE agendamentos SET ultima=$u WHERE rotina=$i AND ativo=1 AND hora=$h AND dias=$d AND (ultima IS NULL OR ultima<>$u)";
        cmd.Parameters.AddWithValue("$u", chave); cmd.Parameters.AddWithValue("$i", item.RotinaId); cmd.Parameters.AddWithValue("$h", item.Hora); cmd.Parameters.AddWithValue("$d", JsonSerializer.Serialize(item.Dias)); return cmd.ExecuteNonQuery() == 1;
    }
    public async Task ExecutarPendentesAsync(DateTimeOffset agora, CancellationToken ct = default)
    {
        if (Interlocked.CompareExchange(ref _executando, 1, 0) != 0) return;
        try {
            foreach (var item in Listar().Where(i => RegraAgendamento.Devido(i, agora))) {
                ct.ThrowIfCancellationRequested();
                var rotina = _catalogo.Listar().FirstOrDefault(r => r.Id == item.RotinaId && r.Ativa);
                if (rotina is null || !Reservar(item, RegraAgendamento.Chave(agora))) continue;
                try {
                    await _executor.ExecutarAsync(rotina.Acoes, new Progress<ProgressoRotina>(), ct);
                    _ultimo = $"{agora:dd/MM HH:mm} · {rotina.Nome}: concluída.";
                }
                catch (OperationCanceledException) { _ultimo = $"{rotina.Nome}: interrompida ao cancelar ou sair do Jarvis."; throw; }
                catch (Exception ex) { _ultimo = $"{rotina.Nome}: não concluída — {ex.Message}"; }
                _historico.Registrar("Rotina: " + rotina.Nome, _ultimo, "agendamento");
            }
        } finally { Interlocked.Exchange(ref _executando, 0); }
    }
    public void Iniciar()
    {
        lock (_vidaGate) {
            if (_timer is not null) return;
            _vida = new(); var token = _vida.Token;
            _timer = new Timer(_ => _ = Tick(token), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15));
        }
    }
    private async Task Tick(CancellationToken ct)
    {
        try { await ExecutarPendentesAsync(DateTimeOffset.Now, ct); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _ultimo = "Agendador: " + ex.Message; }
    }
    public void Parar() { lock (_vidaGate) { _timer?.Dispose(); _timer = null; _vida?.Cancel(); _vida?.Dispose(); _vida = null; } }
}
