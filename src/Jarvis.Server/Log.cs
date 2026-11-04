// JARVIS.Server — log detalhado em %LOCALAPPDATA%\Jarvis\logs (diagnóstico A4/B2).
namespace Jarvis.Server;

/// <summary>Log em arquivo com timestamp. Falhas gravam código bruto + contexto.</summary>
public static class Log
{
    private static readonly object Trava = new();
    private static string Arquivo(string nome = "jarvis.log")
    {
        var pasta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Jarvis", "logs");
        Directory.CreateDirectory(pasta);
        return Path.Combine(pasta, nome);
    }

    public static void Info(string origem, string msg) => Gravar("INFO", origem, msg);
    public static void Aviso(string origem, string msg) => Gravar("AVISO", origem, msg);

    public static void Erro(string origem, string codigoBruto, string msgAmigavel)
    {
        Gravar("ERRO", origem, $"[{codigoBruto}] {msgAmigavel}");
    }

    private static void Gravar(string nivel, string origem, string msg)
    {
        try
        {
            lock (Trava)
                File.AppendAllText(Arquivo(),
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{nivel}] [{origem}] {msg}{Environment.NewLine}");
        }
        catch { }
    }
}

/// <summary>Responde UDP broadcast JARVIS_DISCOVER para redescoberta automática (porta 5187).</summary>
public sealed class DescobertaUdp : IDisposable
{
    private readonly System.Net.Sockets.UdpClient _udp;
    private readonly CancellationTokenSource _cts = new();
    private bool _rodando = true;

    public DescobertaUdp(int portaHttp = 5188, int portaHttps = 5189)
    {
        _udp = new System.Net.Sockets.UdpClient(5187);
        Task.Run(async () =>
        {
            while (_rodando && !_cts.IsCancellationRequested)
            {
                try
                {
                    var r = await _udp.ReceiveAsync(_cts.Token);
                    var texto = System.Text.Encoding.UTF8.GetString(r.Buffer);
                    if (texto.Trim() == "JARVIS_DISCOVER")
                    {
                        var ips = AparelhosIp.Candidatos().Select(x => x.Ip).ToList();
                        var resp = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            app = "Jarvis",
                            ips,
                            http = portaHttp,
                            https = portaHttps,
                            nome = Environment.MachineName,
                        });
                        var bytes = System.Text.Encoding.UTF8.GetBytes(resp);
                        await _udp.SendAsync(bytes, bytes.Length, r.RemoteEndPoint);
                        Log.Info("descoberta", $"respondeu a {r.RemoteEndPoint}");
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) { Log.Aviso("descoberta", ex.Message); }
            }
        });
    }

    public void Dispose()
    {
        _rodando = false;
        _cts.Cancel();
        try { _udp.Close(); } catch { }
        _cts.Dispose();
    }
}
