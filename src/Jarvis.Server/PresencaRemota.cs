namespace Jarvis.Server;
public static class PresencaRemota
{
    private static readonly object Gate = new();
    private static string? _nome;
    private static DateTimeOffset _visto;
    private static int? _bateria;
    private static string _conexao = "Conexão segura";
    public static void Registrar(string nome, string? bateria = null, string? conexao = null)
    {
        lock (Gate)
        {
            if (_nome != nome) { _bateria = null; _conexao = "Conexão segura"; }
            _nome = nome; _visto = DateTimeOffset.UtcNow;
            if (int.TryParse(bateria, out var n) && n is >= 0 and <= 100) _bateria = n;
            if (conexao is "Wi-Fi" or "Rede privada" or "Dados moveis") _conexao = conexao;
        }
    }
    public static string? NomeOnline { get { lock (Gate) return DateTimeOffset.UtcNow - _visto < TimeSpan.FromSeconds(20) ? _nome : null; } }
    public static Jarvis.Core.EstadoCelular? Estado { get { lock (Gate) return DateTimeOffset.UtcNow - _visto < TimeSpan.FromSeconds(20) && _nome is not null ? new(_nome, _bateria, _conexao) : null; } }
}
