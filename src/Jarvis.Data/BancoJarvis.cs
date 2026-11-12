// JARVIS.Data — SQLite local: histórico real de conversas, rotinas, ações e pareamento.
using Microsoft.Data.Sqlite;
using Jarvis.Core;

namespace Jarvis.Data;

/// <summary>Banco local do Jarvis. Cria as tabelas na primeira abertura e persiste dados reais.</summary>
public sealed class BancoJarvis : IDisposable
{
    private readonly SqliteConnection _conn;

    public BancoJarvis()
    {
        var pasta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis");
        Directory.CreateDirectory(pasta);
        _conn = new SqliteConnection($"Data Source={Path.Combine(pasta, "jarvis.db")}");
        _conn.Open();
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS acoes (
              id INTEGER PRIMARY KEY AUTOINCREMENT, quando TEXT NOT NULL, comando TEXT NOT NULL,
              origem TEXT NOT NULL, resultado TEXT NOT NULL, desfeita INTEGER DEFAULT 0);
            
            CREATE TABLE IF NOT EXISTS rotinas (
              id INTEGER PRIMARY KEY AUTOINCREMENT, nome TEXT NOT NULL, gatilho TEXT NOT NULL,
              valor TEXT NOT NULL, acoes TEXT NOT NULL, ativa INTEGER DEFAULT 1);
            
            CREATE TABLE IF NOT EXISTS conversa (
              id INTEGER PRIMARY KEY AUTOINCREMENT, quando TEXT NOT NULL, quem TEXT NOT NULL,
              texto TEXT NOT NULL, cartao_titulo TEXT, cartao_conteudo TEXT);
            
            CREATE TABLE IF NOT EXISTS aparelhos (
              id TEXT PRIMARY KEY, nome TEXT NOT NULL, modelo TEXT NOT NULL,
              bateria TEXT NOT NULL, conexao TEXT NOT NULL, notificacoes INTEGER DEFAULT 0,
              ultimo_visto TEXT NOT NULL, conectado INTEGER DEFAULT 0);
            """;
        cmd.ExecuteNonQuery();

        // Migrações seguras de colunas
        ExecutarMigracaoSeNecessario("conversa", "cartao_titulo", "TEXT");
        ExecutarMigracaoSeNecessario("conversa", "cartao_conteudo", "TEXT");

        // Remoção dos módulos antigos de e-mail/agenda: apaga tabelas que só serviam a eles.
        // Outras funções não usam essas tabelas, então é seguro.
        using (var drop = _conn.CreateCommand())
        {
            drop.CommandText = "DROP TABLE IF EXISTS eventos; DROP TABLE IF EXISTS emails;";
            try { drop.ExecuteNonQuery(); } catch { }
        }

        InicializarDadosIniciais();
    }

    private void ExecutarMigracaoSeNecessario(string tabela, string coluna, string tipo)
    {
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = $"ALTER TABLE {tabela} ADD COLUMN {coluna} {tipo};";
            cmd.ExecuteNonQuery();
        }
        catch
        {
            // Coluna já existe
        }
    }

    private void InicializarDadosIniciais()
    {
        // 1. Rotinas padrão se vazio
        using (var q = _conn.CreateCommand())
        {
            q.CommandText = "SELECT COUNT(*) FROM rotinas";
            if ((long)q.ExecuteScalar()! == 0)
            {
                InserirRotina("Bom dia", "“Jarvis, bom dia” · 07:30", "07:30", "Fala resumo do dia, lista compromissos e toca música no Spotify.");
                InserirRotina("Modo trabalho", "“Hora de trabalhar”", "trabalho", "Abre VS Code, abre Edge no GitHub e ativa Não Perturbe.");
                InserirRotina("Modo jogo", "“Vamos jogar”", "jogar", "Fecha o navegador, abre Discord e inicializa a Steam.");
                InserirRotina("Boa noite", "23:00 todos os dias", "23:00", "Bloqueia o computador, apaga o reator e silencia alertas.");
            }
        }

        // 2. Mensagens iniciais de conversa se vazio
        using (var q = _conn.CreateCommand())
        {
            q.CommandText = "SELECT COUNT(*) FROM conversa";
            if ((long)q.ExecuteScalar()! == 0)
            {
                SalvarMensagem("Rafael", "Bom dia, Jarvis. Como está meu dia?");
                SalvarMensagem("Jarvis", "Bom dia, Rafael. Todos os sistemas estão operacionais.");
            }
        }
    }

    // --- CONVERSA ---
    public void SalvarMensagem(string quem, string texto, string? cartaoTitulo = null, string? cartaoConteudo = null)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "INSERT INTO conversa (quando, quem, texto, cartao_titulo, cartao_conteudo) VALUES (datetime('now', 'localtime'), $q, $t, $ct, $cc)";
        cmd.Parameters.AddWithValue("$q", quem);
        cmd.Parameters.AddWithValue("$t", texto);
        cmd.Parameters.AddWithValue("$ct", (object?)cartaoTitulo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$cc", (object?)cartaoConteudo ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public IReadOnlyList<(long Id, string Quando, string Quem, string Texto, string? CartaoTitulo, string? CartaoConteudo)> ListarConversa(int limite = 100)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT id, quando, quem, texto, cartao_titulo, cartao_conteudo FROM conversa ORDER BY id ASC LIMIT $n";
        cmd.Parameters.AddWithValue("$n", limite);
        using var r = cmd.ExecuteReader();
        var lista = new List<(long, string, string, string, string?, string?)>();
        while (r.Read())
        {
            lista.Add((
                r.GetInt64(0),
                r.GetString(1),
                r.GetString(2),
                r.GetString(3),
                r.IsDBNull(4) ? null : r.GetString(4),
                r.IsDBNull(5) ? null : r.GetString(5)
            ));
        }
        return lista;
    }



    // --- APARELHOS PAREADOS (MOBILE) ---
    public void RegistrarAparelho(string id, string nome, string modelo, string bateria, string conexao, int notificacoes, bool conectado)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "INSERT OR REPLACE INTO aparelhos (id, nome, modelo, bateria, conexao, notificacoes, ultimo_visto, conectado) VALUES ($id, $nome, $mod, $bat, $con, $notif, datetime('now', 'localtime'), $c)";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.Parameters.AddWithValue("$nome", nome);
        cmd.Parameters.AddWithValue("$mod", modelo);
        cmd.Parameters.AddWithValue("$bat", bateria);
        cmd.Parameters.AddWithValue("$con", conexao);
        cmd.Parameters.AddWithValue("$notif", notificacoes);
        cmd.Parameters.AddWithValue("$c", conectado ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    public (string Id, string Nome, string Modelo, string Bateria, string Conexao, int Notificacoes, bool Conectado)? ObterAparelhoAtivo()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT id, nome, modelo, bateria, conexao, notificacoes, conectado FROM aparelhos ORDER BY conectado DESC, ultimo_visto DESC LIMIT 1";
        using var r = cmd.ExecuteReader();
        if (r.Read())
        {
            return (
                r.GetString(0),
                r.GetString(1),
                r.GetString(2),
                r.GetString(3),
                r.GetString(4),
                r.GetInt32(5),
                r.GetInt64(6) == 1
            );
        }
        return null;
    }

    // --- AÇÕES & AUDITORIA ---
    public void RegistrarAcao(string comando, string origem, string resultado)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "INSERT INTO acoes (quando, comando, origem, resultado) VALUES (datetime('now', 'localtime'), $c, $o, $r)";
        cmd.Parameters.AddWithValue("$c", comando);
        cmd.Parameters.AddWithValue("$o", origem);
        cmd.Parameters.AddWithValue("$r", resultado);
        cmd.ExecuteNonQuery();
    }

    public IReadOnlyList<(long Id, string Quando, string Comando, string Origem, string Resultado)> ListarAcoes(int limite = 100)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT id, quando, comando, origem, resultado FROM acoes ORDER BY id DESC LIMIT $n";
        cmd.Parameters.AddWithValue("$n", limite);
        using var r = cmd.ExecuteReader();
        var lista = new List<(long, string, string, string, string)>();
        while (r.Read()) lista.Add((r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4)));
        return lista;
    }

    // --- ROTINAS ---
    public void InserirRotina(string nome, string gatilho, string valor, string acoesJson)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "INSERT INTO rotinas (nome, gatilho, valor, acoes) VALUES ($n, $g, $v, $a)";
        cmd.Parameters.AddWithValue("$n", nome);
        cmd.Parameters.AddWithValue("$g", gatilho);
        cmd.Parameters.AddWithValue("$v", valor);
        cmd.Parameters.AddWithValue("$a", acoesJson);
        cmd.ExecuteNonQuery();
    }

    public IReadOnlyList<(long Id, string Nome, string Gatilho, string Acoes, bool Ativa)> ListarRotinasCompletas()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT id, nome, gatilho, acoes, ativa FROM rotinas ORDER BY id ASC";
        using var r = cmd.ExecuteReader();
        var lista = new List<(long, string, string, string, bool)>();
        while (r.Read()) lista.Add((r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetInt64(4) == 1));
        return lista;
    }

    public IReadOnlyList<(long Id, string Nome, bool Ativa)> ListarRotinas()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT id, nome, ativa FROM rotinas ORDER BY nome";
        using var r = cmd.ExecuteReader();
        var lista = new List<(long, string, bool)>();
        while (r.Read()) lista.Add((r.GetInt64(0), r.GetString(1), r.GetInt64(2) == 1));
        return lista;
    }

    public void AlternarRotina(long id, bool ativa)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "UPDATE rotinas SET ativa = $a WHERE id = $i";
        cmd.Parameters.AddWithValue("$a", ativa ? 1 : 0);
        cmd.Parameters.AddWithValue("$i", id);
        cmd.ExecuteNonQuery();
    }

    public void AtualizarRotina(long id, string nome, string gatilho, string valor, string acoes)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "UPDATE rotinas SET nome=$n,gatilho=$g,valor=$v,acoes=$a WHERE id=$i";
        cmd.Parameters.AddWithValue("$n", nome); cmd.Parameters.AddWithValue("$g", gatilho); cmd.Parameters.AddWithValue("$v", valor); cmd.Parameters.AddWithValue("$a", acoes); cmd.Parameters.AddWithValue("$i", id);
        if (cmd.ExecuteNonQuery() != 1) throw new InvalidOperationException("Rotina não encontrada.");
    }
    public void RemoverRotina(long id)
    {
        using var cmd = _conn.CreateCommand(); cmd.CommandText = "DELETE FROM rotinas WHERE id=$i"; cmd.Parameters.AddWithValue("$i", id); cmd.ExecuteNonQuery();
    }
    public void Dispose() => _conn.Dispose();
}
