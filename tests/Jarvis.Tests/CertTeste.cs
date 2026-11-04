using Jarvis.Server;
using System.Security.Cryptography.X509Certificates;

namespace Jarvis.Tests;

public sealed class CertTeste
{
    [Fact]
    public void CertTemChavePrivada()
    {
        var cert = CertificadoLocal.Gerar("127.0.0.1");
        Assert.True(cert.HasPrivateKey);
        Assert.NotEmpty(CertificadoLocal.Impressao(cert));
    }

    [Fact]
    public void CertAssinaDados()
    {
        var cert = CertificadoLocal.Gerar("127.0.0.1");
        using var rsa = cert.GetRSAPrivateKey();
        Assert.NotNull(rsa);
        var sig = rsa.SignData(new byte[] { 1, 2, 3 },
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);
        Assert.NotEmpty(sig);
    }

    [Fact]
    public async Task KestrelComCertSimples()
    {
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var req = new CertificateRequest("CN=x", rsa,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);
        using var tmp = req.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1));
        var cert = new X509Certificate2(tmp.Export(X509ContentType.Pfx));
        var app = HospedeJarvis.Criar(Array.Empty<string>(), 28768, 18768, cert);
        var t = Task.Run(() => app.StartAsync());
        await Task.Delay(3000);
        try
        {
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
            };
            using var http = new HttpClient(handler);
            using var r = await http.GetAsync("https://127.0.0.1:18768/health");
            Assert.True(r.IsSuccessStatusCode);
        }
        finally
        {
            await app.StopAsync();
        }
        await t;
    }

    [Fact]
    public async Task SslPuroComCertGerar()
    {
        var cert = CertificadoLocal.Gerar("127.0.0.1");
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 18769);
        listener.Start();
        Exception? erroServidor = null;
        var servidor = Task.Run(async () =>
        {
            try
            {
                using var cli = await listener.AcceptTcpClientAsync();
                using var ssl = new System.Net.Security.SslStream(cli.GetStream(), false);
                await ssl.AuthenticateAsServerAsync(cert);
                return ssl.IsAuthenticated;
            }
            catch (Exception ex) { erroServidor = ex; return false; }
        });
        using var cli2 = new System.Net.Sockets.TcpClient();
        await cli2.ConnectAsync("127.0.0.1", 18769);
        using var ssl2 = new System.Net.Security.SslStream(cli2.GetStream(), false,
            (a, b, c, d) => true);
        try { await ssl2.AuthenticateAsClientAsync("localhost"); } catch { }
        var okServidor = await servidor;
        if (erroServidor is not null) throw new Exception("SERVIDOR: " + erroServidor.GetBaseException().Message);
        Assert.True(okServidor);
        listener.Stop();
    }

    [Fact]
    public async Task KestrelRespondeHttps()
    {
        var cert = CertificadoLocal.Gerar("127.0.0.1");
        var app = HospedeJarvis.Criar(Array.Empty<string>(), 28766, 18766, cert);
        var t = Task.Run(() => app.StartAsync());
        await Task.Delay(3000);
        try
        {
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
            };
            using var http = new HttpClient(handler);
            using var r = await http.GetAsync("https://127.0.0.1:18766/health");
            Assert.True(r.IsSuccessStatusCode);
        }
        finally
        {
            await app.StopAsync();
        }
        await t;
    }

    [Fact]
    public async Task KestrelRespondeHttp()
    {
        var app = HospedeJarvis.Criar(Array.Empty<string>(), 18767, 28767, null);
        var t = Task.Run(() => app.StartAsync());
        await Task.Delay(3000);
        try
        {
            using var http = new HttpClient();
            using var r = await http.GetAsync("http://127.0.0.1:18767/health");
            Assert.True(r.IsSuccessStatusCode);
        }
        finally
        {
            await app.StopAsync();
        }
        await t;
    }
}
