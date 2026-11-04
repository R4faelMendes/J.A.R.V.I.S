// JARVIS.Server — pareamento por QR, comandos fixos, certificado local.
// O celular NUNCA executa texto livre: só esta lista fechada.
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Jarvis.Server;

/// <summary>Comandos que o celular pode pedir. Nada fora desta lista executa.</summary>
public static class ComandosFixos
{
    public static readonly HashSet<string> Permitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "abrir_app", "fechar_app", "fechar_janela", "bloquear", "suspender", "reiniciar",
        "ler_desempenho", "falar", "midia_pausar", "midia_continuar", "midia_proxima", "midia_anterior", "midia_alternar", "volume", "layout", "minimizar_janela", "maximizar_janela", "focar_janela",
    };
}

/// <summary>Pareamento: QR com endereço, porta, token único e impressão do certificado.</summary>
public sealed class PareamentoService
{
    private readonly Dictionary<string, (string Token, DateTime Expira)> _codigos = new();
    private readonly Dictionary<string, DateTime> _tokens = new();
    private readonly object _trava = new();

    public string GerarCodigo6()
    {
        var rnd = RandomNumberGenerator.GetInt32(0, 1000000);
        return rnd.ToString("000000");
    }

    /// <summary>Emite um par código de 6 dígitos e token de uso único válidos por 5 minutos.</summary>
    public void EmitirPar(out string codigo6, out string token)
    {
        codigo6 = GerarCodigo6();
        token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var expira = DateTime.UtcNow.AddMinutes(5);
        lock (_trava)
        {
            _codigos[codigo6] = (token, expira);
            _tokens[token] = expira;
        }
    }

    /// <summary>Emite um token de uso único válido por 5 minutos.</summary>
    public string EmitirToken()
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        lock (_trava) _tokens[token] = DateTime.UtcNow.AddMinutes(5);
        return token;
    }

    public bool ValidarToken(string token)
    {
        lock (_trava)
        {
            if (_tokens.TryGetValue(token, out var exp) && exp > DateTime.UtcNow)
            {
                _tokens.Remove(token);
                foreach (var codigo in _codigos.Where(x => x.Value.Token == token).Select(x => x.Key).ToArray()) _codigos.Remove(codigo);
                return true;
            }
            return false;
        }
    }

    public bool ValidarCodigo(string codigo6, out string token)
    {
        token = "";
        lock (_trava)
        {
            var limpo = codigo6.Trim().Replace(" ", "");
            if (_codigos.TryGetValue(limpo, out var par) && par.Expira > DateTime.UtcNow)
            {
                _codigos.Remove(limpo);
                _tokens.Remove(par.Token);
                token = par.Token;
                return true;
            }
            return false;
        }
    }

    /// <summary>Payload do QR: url|porta|token|sha256-do-cert.</summary>
    public static string MontarQr(string ip, int porta, string token, string impressao) =>
        $"jarvis://parear?ip={ip}&porta={porta}&token={token}&pin={impressao}";

    public static byte[] QrPng(string conteudo)
    {
        using var gen = new QRCoder.QRCodeGenerator();
        using var dados = gen.CreateQrCode(conteudo, QRCoder.QRCodeGenerator.ECCLevel.M);
        using var qr = new QRCoder.PngByteQRCode(dados);
        return qr.GetGraphic(pixelsPerModule: 8);
    }
}

/// <summary>Certificado autoassinado local + impressão digital (pinning no celular).</summary>
public static class CertificadoLocal
{
    /// <summary>Cria uma vez e reutiliza (impressão estável entre reinícios).</summary>
    public static X509Certificate2 ObterOuCriar(string pasta)
    {
        Directory.CreateDirectory(pasta);
        var arq = Path.Combine(pasta, "jarvis-cert.pfx");
        if (File.Exists(arq))
        {
            try { return new X509Certificate2(File.ReadAllBytes(arq)); }
            catch { try { File.Delete(arq); } catch { } }
        }
        var (cert, pfx) = GerarComPfx();
        File.WriteAllBytes(arq, pfx);
        return cert;
    }

    private static (X509Certificate2 Cert, byte[] Pfx) GerarComPfx()
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=Jarvis-Local", rsa,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var tmp = req.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(2));
        var pfx = tmp.Export(X509ContentType.Pfx);
        return (new X509Certificate2(pfx), pfx);
    }

    public static X509Certificate2 Gerar(string ip)
    {
        // Round-trip PFX sem flags: persiste no user store e o Schannel aceita.
        // (EphemeralKeySet quebra o handshake TLS neste Windows; chave estática também falhou.)
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=Jarvis-Local", rsa,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        // Sem extensões: BC/KU/EKU quebraram o handshake TLS neste Windows.
        // Validação no celular é por impressão digital (pinning), não por cadeia.
        using var tmp = req.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(2));
        return new X509Certificate2(tmp.Export(X509ContentType.Pfx));
    }

    public static string Impressao(X509Certificate2 cert) =>
        Convert.ToHexString(SHA256.HashData(cert.RawData));
}
