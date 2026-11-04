using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
namespace Jarvis.Core;

public static class EnderecosJarvis
{
    public static string? Normalizar(string? value)
    {
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.UserInfo.Length > 0 || uri.AbsolutePath != "/" || uri.Query.Length > 0 || uri.Fragment.Length > 0) return null;
        return uri.GetLeftPart(UriPartial.Authority);
    }
    public static bool Tailscale(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Host.EndsWith(".ts.net", StringComparison.OrdinalIgnoreCase) || IPAddress.TryParse(uri.Host, out var ip) && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && ip.GetAddressBytes() is var b && b[0] == 100 && b[1] is >= 64 and <= 127);
    public static string[] Reunir(params IEnumerable<string?>[] sources) => sources.SelectMany(s => s).Select(Normalizar).Where(s => s is not null).Cast<string>().Distinct(StringComparer.OrdinalIgnoreCase).Take(12).ToArray();
}
public sealed record SondagemPc(string Endereco, bool Conectado, string Mensagem, IdentidadePc? Identidade = null);
public static class ConexaoPareada
{
    public static async Task<SondagemPc> VerificarAsync(string endereco, string pin, string token, CancellationToken ct = default)
    {
        var url = EnderecosJarvis.Normalizar(endereco);
        if (url is null || pin.Length != 64 || !pin.All(Uri.IsHexDigit) || string.IsNullOrWhiteSpace(token)) return new(endereco, false, "Pareamento incompleto. Leia o QR de pareamento novamente.");
        var certificadoDiferente = false;
        using var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, cert, _, _) =>
        {
            var ok = cert is not null && Convert.ToHexString(SHA256.HashData(cert.RawData)).Equals(pin, StringComparison.OrdinalIgnoreCase); if (!ok) certificadoDiferente = true; return ok;
        } };
        using var http = new HttpClient(handler) { BaseAddress = new Uri(url), Timeout = TimeSpan.FromSeconds(3) };
        http.DefaultRequestHeaders.Add("X-Jarvis-Token", token);
        try
        {
            using var response = await http.GetAsync("/api/identidade", ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized) return new(url, false, "Token recusado pelo PC. Leia um novo QR de pareamento.");
            if (!response.IsSuccessStatusCode) return new(url, false, $"O servidor respondeu HTTP {(int)response.StatusCode}.");
            var identity = await response.Content.ReadFromJsonAsync<IdentidadePc>(cancellationToken: ct);
            return identity is null ? new(url, false, "Identidade do PC indisponível.") : new(url, true, RedeDireta.Publico(url) ? "Conectado diretamente ao PC pela internet." : "Conectado diretamente ao PC.", identity);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (OperationCanceledException) { return new(url, false, "PC sem resposta: confira energia, Jarvis aberto e acesso de rede ao endereço direto."); }
        catch (HttpRequestException) { return new(url, false, certificadoDiferente ? "O certificado não corresponde ao PC pareado. A conexão foi bloqueada; confira o QR do PC." : "Não alcancei o servidor. Tentarei outro endereço salvo."); }
        catch (System.Text.Json.JsonException) { return new(url, false, "O PC enviou uma identidade inválida. Atualize o Jarvis do computador."); }
        catch (IOException) { return new(url, false, "A conexão foi interrompida durante a verificação. Tentarei outro endereço."); }
    }
    public static async Task<SondagemPc[]> ProcurarAsync(IEnumerable<string> enderecos, string pin, string token, CancellationToken ct = default)
    {
        var urls = EnderecosJarvis.Reunir(enderecos); using var concurrency = new SemaphoreSlim(3); using var life = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var tasks = urls.Select(async url => { try { await concurrency.WaitAsync(life.Token); try { return await VerificarAsync(url, pin, token, life.Token); } finally { concurrency.Release(); } } catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return new SondagemPc(url, false, "Busca concluída em outro endereço."); } }).ToList();
        var results = new List<SondagemPc>(); var pending = tasks.ToList();
        try
        {
            while (pending.Count > 0)
            {
                var done = await Task.WhenAny(pending); pending.Remove(done); var result = await done; results.Add(result);
                if (result.Conectado) { life.Cancel(); break; }
            }
        }
        finally { life.Cancel(); try { await Task.WhenAll(tasks); } catch (OperationCanceledException) { } }
        ct.ThrowIfCancellationRequested(); return results.ToArray();
    }
}
