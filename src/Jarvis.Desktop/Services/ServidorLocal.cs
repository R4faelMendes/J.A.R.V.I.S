using System.IO;
using Jarvis.Server;
using Microsoft.Extensions.Hosting;

namespace Jarvis.Desktop.Services;

/// <summary>Inicia e encerra o servidor local sem trabalho de rede na interface.</summary>
public sealed class ServidorLocal : IDisposable
{
    private CancellationTokenSource? _vida;
    private Task? _execucao;
    private volatile bool _ativo;

    public bool Ativo => _ativo;

    public void Iniciar()
    {
        if (_execucao is { IsCompleted: false }) return;
        _vida?.Dispose();
        _vida = new CancellationTokenSource();
        var ct = _vida.Token;
        _execucao = Task.Run(() => ExecutarAsync(ct));
    }

    private async Task ExecutarAsync(CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var pasta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis");
            using var cert = CertificadoLocal.ObterOuCriar(pasta);
            CertificadoCache.Impressao = CertificadoLocal.Impressao(cert);
            await using var app = HospedeJarvis.Criar([], HospedeJarvis.PortaHttp, HospedeJarvis.PortaHttps,
                cert, new ControleRemotoPc(), Path.Combine(AppContext.BaseDirectory, "mobile", "Jarvis.apk"),
                (CentralAparelhos)ServicosAplicacao.Aparelhos, ServicosAplicacao.CredenciaisCelulares);

            await app.StartAsync(ct).ConfigureAwait(false);
            _ativo = true;
            LogServidor($"Servidor pronto: HTTPS {HospedeJarvis.PortaHttps}; pareamento direto no app.");
            DescobertaUdp? descoberta = null;
            try
            {
                try { descoberta = new DescobertaUdp(HospedeJarvis.PortaHttp, HospedeJarvis.PortaHttps); }
                catch (Exception ex) { LogServidor("Descoberta UDP indisponível; QR e PIN continuam ativos: " + ex.GetType().Name); }
                using var fim = CancellationTokenSource.CreateLinkedTokenSource(ct, app.Lifetime.ApplicationStopping);
                try { await Task.Delay(Timeout.InfiniteTimeSpan, fim.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) when (fim.IsCancellationRequested) { }
            }
            finally
            {
                _ativo = false;
                descoberta?.Dispose();
                using var prazo = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await app.StopAsync(prazo.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { LogServidor("Falha no servidor local: " + ex.GetBaseException().Message); }
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
        if (_vida is null) return;
        var vida = _vida;
        _vida = null;
        vida.Cancel();
        try { _execucao?.WaitAsync(TimeSpan.FromSeconds(4)).GetAwaiter().GetResult(); }
        catch (TimeoutException) { LogServidor("Encerramento: tempo de espera excedido."); }
        finally { vida.Dispose(); }
    }

    public void Dispose() => Parar();
}
