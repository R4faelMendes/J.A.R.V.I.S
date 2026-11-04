// JARVIS Desktop — servidor local embutido (Kestrel + SignalR na thread de fundo).
using System.IO;
using Jarvis.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Jarvis.Desktop.Services;

/// <summary>Sobe o servidor do celular junto com o app. Nunca trava a UI.</summary>
public sealed class ServidorLocal : IDisposable
{
    private WebApplication? _app;
    private Thread? _fio;
    private DescobertaUdp? _descoberta;
    public bool Ativo => _app is not null;

    public void Iniciar()
    {
        if (Ativo) return;
        try
        {
            var pasta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis");
            var cert = CertificadoLocal.ObterOuCriar(pasta);
            CertificadoCache.Impressao = CertificadoLocal.Impressao(cert);
            _app = HospedeJarvis.Criar(Array.Empty<string>(), HospedeJarvis.PortaHttp, HospedeJarvis.PortaHttps, cert, new ControleRemotoPc(), Path.Combine(AppContext.BaseDirectory, "mobile", "Jarvis.apk"), (CentralAparelhos)ServicosAplicacao.Aparelhos);
        }
        catch (Exception ex)
        {
            LogServidor("FALHA ao montar: " + ex.GetBaseException().Message);
            _app = null;
            return;
        }
        var alvo = _app;
        _fio = new Thread(() =>
        {
            try
            {
                alvo.StartAsync().GetAwaiter().GetResult();
                LogServidor($"Servidor pronto: HTTPS {HospedeJarvis.PortaHttps}; pareamento direto no app.");
                try { _descoberta = new DescobertaUdp(HospedeJarvis.PortaHttp, HospedeJarvis.PortaHttps); }
                catch (Exception ex) { LogServidor("Descoberta UDP indisponível; QR e PIN continuam ativos: " + ex.GetType().Name); }
                alvo.WaitForShutdown();
            }
            catch (Exception ex) { LogServidor("FALHA ao rodar: " + ex.GetBaseException().Message); }
        })
        { IsBackground = true, Name = "Jarvis-Server" };
        _fio.Start();
    }

    internal static void LogServidor(string linha)
    {
        try
        {
            var pasta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis");
            Directory.CreateDirectory(pasta);
            File.AppendAllText(Path.Combine(pasta, "server.log"),
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {linha}{Environment.NewLine}");
        }
        catch { }
    }

    public void Parar()
    {
        var app = _app;
        _app = null;
        if (app is null) return;
        try
        {
            // StopAsync pode capturar o Dispatcher do WPF. Nunca bloquear essa mesma thread
            // aguardando uma continuação que precisa dela para terminar.
            Task.Run(async () =>
            {
                using var prazo = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await app.StopAsync(prazo.Token).ConfigureAwait(false);
            }).WaitAsync(TimeSpan.FromSeconds(4)).GetAwaiter().GetResult();
        }
        catch (Exception ex) { LogServidor("Encerramento: " + ex.GetType().Name); }
    }
    public void Dispose()
    {
        Parar();
        try { _descoberta?.Dispose(); } catch { }
        _descoberta = null;
    }
}
