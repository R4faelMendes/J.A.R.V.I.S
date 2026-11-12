using Jarvis.Core;
using Microsoft.Data.Sqlite;
namespace Jarvis.Data;

public sealed class HistoricoComandos : IHistoricoComandos
{
    private readonly string _arquivo;
    public event EventHandler? Alterado;
    public HistoricoComandos(string? arquivo = null)
    {
        _arquivo = arquivo ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis", "jarvis.db");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_arquivo))!);
        using var db = Abrir(); using var cmd = db.CreateCommand();
        cmd.CommandText = "CREATE TABLE IF NOT EXISTS comandos (id INTEGER PRIMARY KEY AUTOINCREMENT, quando TEXT NOT NULL, comando TEXT NOT NULL, resposta TEXT NOT NULL, origem TEXT NOT NULL)";
        cmd.ExecuteNonQuery();
    }
    private SqliteConnection Abrir() { var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _arquivo, DefaultTimeout = 5 }.ToString()); db.Open(); return db; }
    public IReadOnlyList<RegistroComando> Listar(int limite = 50)
    {
        using var db = Abrir(); using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT id, quando, comando, resposta, origem FROM comandos ORDER BY id DESC LIMIT $n";
        cmd.Parameters.AddWithValue("$n", Math.Clamp(limite, 1, 100));
        using var reader = cmd.ExecuteReader(); var result = new List<RegistroComando>();
        while (reader.Read()) result.Add(new(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4)));
        return result;
    }
    public void Registrar(string comando, string resposta, string origem)
    {
        if (string.IsNullOrWhiteSpace(comando)) return;
        using var db = Abrir(); using var transaction = db.BeginTransaction(); using var cmd = db.CreateCommand(); cmd.Transaction = transaction;
        cmd.CommandText = "INSERT INTO comandos (quando,comando,resposta,origem) VALUES ($q,$c,$r,$o); DELETE FROM comandos WHERE id NOT IN (SELECT id FROM comandos ORDER BY id DESC LIMIT 200)";
        cmd.Parameters.AddWithValue("$q", DateTimeOffset.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$c", comando[..Math.Min(comando.Length, 4000)]);
        cmd.Parameters.AddWithValue("$r", resposta[..Math.Min(resposta.Length, 12000)]);
        cmd.Parameters.AddWithValue("$o", origem); cmd.ExecuteNonQuery(); transaction.Commit(); Alterado?.Invoke(this, EventArgs.Empty);
    }
    public void Limpar() { using var db = Abrir(); using var cmd = db.CreateCommand(); cmd.CommandText = "DELETE FROM comandos"; cmd.ExecuteNonQuery(); Alterado?.Invoke(this, EventArgs.Empty); }
}
