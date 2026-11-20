using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Jarvis.Mobile.Services;

public sealed class ContaJarvis
{
    private const string ChaveSessao = "jarvis_conta_v1";
    private static readonly Uri? Origem = ObterOrigem();
    private static readonly HttpClient Http = new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        BaseAddress = Origem,
        Timeout = TimeSpan.FromSeconds(12)
    };

    public static bool Disponivel => Origem is not null;

    public async Task<Sessao?> SessaoAsync() =>
        JsonSerializer.Deserialize<Sessao>(await SecureStorage.GetAsync(ChaveSessao) ?? "null");

    public async Task EntrarAsync(CancellationToken cancellationToken)
    {
        if (Origem is null) throw new InvalidOperationException("A API de conta ainda não está disponível.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(5));
        var ct = timeout.Token;
        var proof = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(proof)));
        using var inicio = await Http.PostAsJsonAsync("/v1/login", new { Proof = hash }, ct);
        inicio.EnsureSuccessStatusCode();
        var login = await inicio.Content.ReadFromJsonAsync<InicioLogin>(cancellationToken: ct);
        if (login is null || login.Url.GetLeftPart(UriPartial.Authority) != Origem.GetLeftPart(UriPartial.Authority))
            throw new InvalidOperationException("Endereço de login inválido.");
        if (!await Browser.Default.OpenAsync(login.Url, BrowserLaunchMode.External))
            throw new InvalidOperationException("Não foi possível abrir o navegador.");
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        while (await timer.WaitForNextTickAsync(ct))
        {
            using var resposta = await Http.PostAsJsonAsync("/v1/login/" + Uri.EscapeDataString(login.Id) + "/token", new { Proof = proof }, ct);
            resposta.EnsureSuccessStatusCode();
            if (resposta.StatusCode == HttpStatusCode.NoContent) continue;
            var sessao = await resposta.Content.ReadFromJsonAsync<Sessao>(cancellationToken: ct);
            await SecureStorage.SetAsync(ChaveSessao, JsonSerializer.Serialize(sessao ?? throw new InvalidOperationException("Sessão vazia.")));
            return;
        }
    }

    public async Task<IReadOnlyList<PcConta>> ListarAsync(CancellationToken ct)
    {
        if (await SessaoAsync() is not { } sessao) return [];
        using var resposta = await EnviarAsync(HttpMethod.Get, "/v1/pcs", sessao, null, ct);
        return await resposta.Content.ReadFromJsonAsync<List<PcConta>>(cancellationToken: ct) ?? [];
    }

    public async Task SalvarAsync(PcConta pc, CancellationToken ct = default)
    {
        if (Disponivel && await SessaoAsync() is { } sessao)
        {
            using var resposta = await EnviarAsync(HttpMethod.Put, "/v1/pcs/" + Uri.EscapeDataString(pc.Id), sessao, JsonContent.Create(new { pc.Name }), ct);
        }
    }

    public async Task RemoverAsync(string id, CancellationToken ct)
    {
        if (await SessaoAsync() is not { } sessao) return;
        using var resposta = await EnviarAsync(HttpMethod.Delete, "/v1/pcs/" + Uri.EscapeDataString(id), sessao, null, ct);
    }

    public async Task SairAsync(CancellationToken ct)
    {
        if (await SessaoAsync() is { } sessao)
        {
            using var resposta = await EnviarAsync(HttpMethod.Delete, "/v1/session", sessao, null, ct);
        }
        SecureStorage.Remove(ChaveSessao);
    }

    private async Task<HttpResponseMessage> EnviarAsync(HttpMethod metodo, string caminho, Sessao sessao, HttpContent? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(metodo, caminho) { Content = body };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessao.Token);
        var resposta = await Http.SendAsync(request, ct);
        if (resposta.IsSuccessStatusCode) return resposta;
        var status = resposta.StatusCode;
        resposta.Dispose();
        if (status == HttpStatusCode.Unauthorized)
        {
            if ((await SessaoAsync())?.Token == sessao.Token) SecureStorage.Remove(ChaveSessao);
            throw new InvalidOperationException("Sua sessão expirou. Entre novamente com o Google.");
        }
        throw new HttpRequestException("A API de conta não concluiu a solicitação.", null, status);
    }

    private static Uri? ObterOrigem()
    {
        var value = typeof(ContaJarvis).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "JarvisAccountsOrigin")?.Value;
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == "https" &&
            uri.AbsolutePath == "/" && uri.Query.Length == 0 && uri.UserInfo.Length == 0 && uri.Fragment.Length == 0 ? uri : null;
    }

    public sealed record Sessao(string Token, string? Email, string AccountId);
    private sealed record InicioLogin(string Id, Uri Url);
}

public sealed record PcConta(string Id, string Name);
