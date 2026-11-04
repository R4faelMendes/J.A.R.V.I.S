using System.Text.Json;
using Jarvis.Core;
using Microsoft.Data.Sqlite;
namespace Jarvis.Data;
public sealed class AgendaService : IAgendaService
{
    private readonly string _connection;
    private readonly object _gate = new();
    public AgendaService(string? arquivo = null)
    {
        arquivo ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis", "jarvis.db");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(arquivo))!);
        _connection = new SqliteConnectionStringBuilder { DataSource = arquivo }.ToString();
        using var db = Abrir(); using var cmd = db.CreateCommand();
        cmd.CommandText = "CREATE TABLE IF NOT EXISTS agenda_local (id TEXT PRIMARY KEY, versao INTEGER NOT NULL, dados TEXT NOT NULL)"; cmd.ExecuteNonQuery();
    }
    private SqliteConnection Abrir() { var db = new SqliteConnection(_connection); db.Open(); return db; }
    public IReadOnlyList<Compromisso> Listar()
    {
        lock (_gate) { using var db = Abrir(); using var cmd = db.CreateCommand(); cmd.CommandText = "SELECT dados FROM agenda_local"; using var reader = cmd.ExecuteReader(); var items = new List<Compromisso>(); while (reader.Read()) { var item = JsonSerializer.Deserialize<Compromisso>(reader.GetString(0)); if (item is not null) items.Add(item); } return items.OrderBy(c => c.Inicio).ToArray(); }
    }
    public Compromisso Salvar(Compromisso compromisso)
    {
        compromisso.Validar();
        lock (_gate)
        {
            using var db = Abrir(); using var tx = db.BeginTransaction();
            using var read = db.CreateCommand(); read.Transaction = tx; read.CommandText = "SELECT versao FROM agenda_local WHERE id=$id"; read.Parameters.AddWithValue("$id", compromisso.Id); var current = read.ExecuteScalar();
            var version = current is null ? 0 : Convert.ToInt64(current);
            if (version != compromisso.Versao) throw new ConflitoAgendaException();
            using var prior = db.CreateCommand(); prior.Transaction = tx; prior.CommandText = "SELECT dados FROM agenda_local WHERE id=$id"; prior.Parameters.AddWithValue("$id", compromisso.Id);
            var previous = prior.ExecuteScalar() is string json ? JsonSerializer.Deserialize<Compromisso>(json) : null;
            if (previous is null || previous.Inicio != compromisso.Inicio || previous.Fim != compromisso.Fim) AgendaDatas.Validar(compromisso, DateTime.Today);
            var saved = compromisso with { Titulo = compromisso.Titulo.Trim(), Versao = version + 1 };
            using var write = db.CreateCommand(); write.Transaction = tx; write.CommandText = "INSERT INTO agenda_local(id,versao,dados) VALUES($id,$v,$d) ON CONFLICT(id) DO UPDATE SET versao=$v,dados=$d";
            write.Parameters.AddWithValue("$id", saved.Id); write.Parameters.AddWithValue("$v", saved.Versao); write.Parameters.AddWithValue("$d", JsonSerializer.Serialize(saved)); write.ExecuteNonQuery(); tx.Commit(); return saved;
        }
    }
}
