using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Jarvis.Core;

namespace Jarvis.Ai;

/// <summary>OAuth desktop com PKCE, callback somente loopback e autorização criptografada por usuário.</summary>
public sealed class GoogleMailService : IEmailService
{
    private static readonly HttpClient HttpPadrao = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly HttpClient _http;
    private readonly Action<Uri> _abrirNavegador;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _arquivo;
    private Credenciais _dados;
    private sealed record Credenciais(string ClientId = "", string ClientSecret = "", string RefreshToken = "", string AccessToken = "", DateTimeOffset Expira = default);
    public GoogleMailService(string? pasta = null, HttpClient? http = null, Action<Uri>? abrirNavegador = null)
    {
        _http = http ?? HttpPadrao;
        _abrirNavegador = abrirNavegador ?? (url => Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true }));
        pasta ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis");
        Directory.CreateDirectory(pasta);
        _arquivo = Path.Combine(pasta, "google-mail.dpapi");
        _dados = Ler();
        if (!File.Exists(_arquivo) && Configurado) Salvar();
    }
    public bool Configurado => !string.IsNullOrWhiteSpace(_dados.ClientId);
    public bool Conectado => !string.IsNullOrWhiteSpace(_dados.RefreshToken);
    private Credenciais Ler()
    {
        try { return JsonSerializer.Deserialize<Credenciais>(ProtectedData.Unprotect(File.ReadAllBytes(_arquivo), null, DataProtectionScope.CurrentUser)) ?? new(); }
        catch
        {
            // Reaproveita apenas o cadastro desktop protegido de versões anteriores.
            // Senhas IMAP e autorizações antigas não são migradas.
            foreach (var nome in new[] { "client_secret.dat", "google_creds.dat" })
            {
                byte[]? claro = null;
                try
                {
                    claro = ProtectedData.Unprotect(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(_arquivo)!, nome)), null, DataProtectionScope.CurrentUser);
                    using var doc = JsonDocument.Parse(claro);
                    var r = doc.RootElement;
                    var desktop = r.TryGetProperty("installed", out var app);
                    var id = desktop ? app.GetProperty("client_id").GetString() : r.GetProperty("ClientId").GetString();
                    var secret = desktop ? app.GetProperty("client_secret").GetString() : r.GetProperty("ClientSecret").GetString();
                    if (id?.EndsWith(".apps.googleusercontent.com", StringComparison.Ordinal) == true) return new(id, secret ?? "");
                }
                catch { }
                finally { if (claro is not null) CryptographicOperations.ZeroMemory(claro); }
            }
            return new();
        }
    }
    private void Salvar()
    {
        var bytes = ProtectedData.Protect(JsonSerializer.SerializeToUtf8Bytes(_dados), null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(_arquivo + ".tmp", bytes);
        File.Move(_arquivo + ".tmp", _arquivo, true);
    }
    public async Task ConfigurarClienteAsync(string arquivoJson)
    {
        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(arquivoJson));
        if (!doc.RootElement.TryGetProperty("installed", out var app))
            throw new InvalidOperationException("Escolha o JSON de um cliente OAuth do tipo Aplicativo para computador.");
        var id = app.GetProperty("client_id").GetString() ?? "";
        if (!id.EndsWith(".apps.googleusercontent.com", StringComparison.Ordinal)) throw new InvalidOperationException("O arquivo não contém um cliente Google válido.");
        var secret = app.TryGetProperty("client_secret", out var s) ? s.GetString() ?? "" : "";
        await _gate.WaitAsync();
        try { _dados = _dados.ClientId == id ? _dados with { ClientSecret = secret } : new(id, secret); Salvar(); }
        finally { _gate.Release(); }
    }
    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    public async Task ConectarAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (!Configurado) throw new InvalidOperationException("Configure primeiro o cliente Google na opção Configuração do desenvolvedor.");
            using var prazo = CancellationTokenSource.CreateLinkedTokenSource(ct);
            prazo.CancelAfter(TimeSpan.FromMinutes(5));
            var token = prazo.Token;
            var state = Base64Url(RandomNumberGenerator.GetBytes(32));
            var verifier = Base64Url(RandomNumberGenerator.GetBytes(48));
            var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var redirect = $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/oauth2callback";
            var parametros = new Dictionary<string, string>
            {
                ["client_id"] = _dados.ClientId, ["redirect_uri"] = redirect, ["response_type"] = "code",
                ["scope"] = "https://www.googleapis.com/auth/gmail.readonly", ["access_type"] = "offline",
                ["prompt"] = "consent", ["state"] = state, ["code_challenge"] = challenge, ["code_challenge_method"] = "S256"
            };
            var url = "https://accounts.google.com/o/oauth2/v2/auth?" + string.Join("&", parametros.Select(x => Uri.EscapeDataString(x.Key) + "=" + Uri.EscapeDataString(x.Value)));
            _abrirNavegador(new Uri(url));
            string? code = null;
            while (code is null)
            {
                using var client = await listener.AcceptTcpClientAsync(token);
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true);
                using var requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                requestTimeout.CancelAfter(TimeSpan.FromSeconds(5));
                string? line;
                try { line = await reader.ReadLineAsync(requestTimeout.Token); }
                catch (OperationCanceledException) when (!token.IsCancellationRequested) { continue; }
                var parts = line?.Split(' ');
                var valid = parts is { Length: 3 } && parts[0] == "GET" && parts[1].StartsWith("/oauth2callback?", StringComparison.Ordinal);
                var query = new Dictionary<string, string>();
                if (valid)
                {
                    foreach (var item in parts![1].Split('?', 2)[1].Split('&'))
                    {
                        var kv = item.Split('=', 2);
                        if (kv.Length == 2) query[Uri.UnescapeDataString(kv[0])] = Uri.UnescapeDataString(kv[1].Replace('+', ' '));
                    }
                    valid = query.GetValueOrDefault("state") == state;
                }
                var body = Encoding.UTF8.GetBytes(valid ? "<!doctype html><meta charset=utf-8><title>Jarvis</title><h1>Volte ao Jarvis</h1><p>A autorização será concluída no aplicativo. Você pode fechar esta aba.</p>" : "Requisição inválida.");
                var head = Encoding.ASCII.GetBytes($"HTTP/1.1 {(valid ? "200 OK" : "400 Bad Request")}\r\nContent-Type: text/html; charset=utf-8\r\nContent-Security-Policy: default-src 'none'\r\nConnection: close\r\nContent-Length: {body.Length}\r\n\r\n");
                await stream.WriteAsync(head, token); await stream.WriteAsync(body, token);
                if (!valid) continue;
                if (query.ContainsKey("error")) throw new InvalidOperationException("A autorização foi cancelada no Google. Sua conta não foi conectada.");
                code = query.GetValueOrDefault("code");
            }
            await TrocarTokenAsync(new() { ["grant_type"] = "authorization_code", ["code"] = code, ["redirect_uri"] = redirect, ["code_verifier"] = verifier }, token);
            if (!Conectado) throw new InvalidOperationException("O Google não enviou autorização para reconectar. Tente Entrar com Google novamente.");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new TimeoutException("O login não foi concluído em cinco minutos. Tente novamente."); }
        finally { _gate.Release(); }
    }
    private async Task TrocarTokenAsync(Dictionary<string, string> dados, CancellationToken ct)
    {
        dados["client_id"] = _dados.ClientId;
        if (_dados.ClientSecret.Length > 0) dados["client_secret"] = _dados.ClientSecret;
        using var resposta = await _http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(dados), ct);
        if (!resposta.IsSuccessStatusCode)
        {
            if (resposta.StatusCode == HttpStatusCode.BadRequest)
            { _dados = _dados with { AccessToken = "", RefreshToken = "" }; Salvar(); }
            throw new InvalidOperationException("Não foi possível renovar o acesso Google. Verifique o cliente OAuth e entre novamente.");
        }
        using var doc = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync(ct));
        var r = doc.RootElement;
        _dados = _dados with
        {
            AccessToken = r.GetProperty("access_token").GetString()!,
            RefreshToken = r.TryGetProperty("refresh_token", out var refresh) ? refresh.GetString()! : dados["grant_type"] == "refresh_token" ? _dados.RefreshToken : "",
            Expira = DateTimeOffset.UtcNow.AddSeconds(r.GetProperty("expires_in").GetInt32() - 60)
        };
        Salvar();
    }
    private async Task<JsonDocument> GetAsync(string recurso, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://gmail.googleapis.com/gmail/v1/users/me/" + recurso);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _dados.AccessToken);
        using var response = await _http.SendAsync(request, ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        { _dados = _dados with { AccessToken = "", Expira = default }; throw new InvalidOperationException("A sessão Google expirou. Sincronize novamente ou entre com Google."); }
        if (response.StatusCode == HttpStatusCode.Forbidden) throw new InvalidOperationException("O Google negou a leitura. Confirme que Gmail API está habilitada e que a permissão de leitura foi autorizada.");
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Gmail indisponível (HTTP {(int)response.StatusCode}). Tente novamente.");
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
    }
    public async Task<CaixaEntrada> SincronizarAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (!Conectado) throw new InvalidOperationException("Entre com Google para ler sua caixa de entrada.");
            if (_dados.Expira <= DateTimeOffset.UtcNow)
                await TrocarTokenAsync(new() { ["grant_type"] = "refresh_token", ["refresh_token"] = _dados.RefreshToken }, ct);
            using var label = await GetAsync("labels/INBOX", ct);
            using var lista = await GetAsync("messages?labelIds=INBOX&maxResults=20", ct);
            var mensagens = new List<EmailResumo>();
            if (lista.RootElement.TryGetProperty("messages", out var itens))
            {
                // Poucas requisições em paralelo para manter a sincronização leve.
                foreach (var lote in itens.EnumerateArray().Select(x => x.GetProperty("id").GetString()!).Chunk(4))
                {
                    var resultados = await Task.WhenAll(lote.Select(async id =>
                    {
                        using var d = await GetAsync($"messages/{Uri.EscapeDataString(id)}?format=metadata&metadataHeaders=From&metadataHeaders=Subject", ct);
                        return LerMensagem(d.RootElement);
                    }));
                    mensagens.AddRange(resultados);
                }
            }
            return new(mensagens.OrderByDescending(m => m.Data).ToArray(), label.RootElement.GetProperty("messagesTotal").GetInt32(), label.RootElement.GetProperty("messagesUnread").GetInt32());
        }
        finally { _gate.Release(); }
    }
    public static EmailResumo LerMensagem(JsonElement r)
    {
        var headers = r.GetProperty("payload").GetProperty("headers").EnumerateArray().ToArray();
        string Header(string name) => headers.FirstOrDefault(h => h.GetProperty("name").GetString()!.Equals(name, StringComparison.OrdinalIgnoreCase)) is var h && h.ValueKind != JsonValueKind.Undefined ? h.GetProperty("value").GetString() ?? "" : "";
        var remetente = Header("From");
        var pos = remetente.IndexOf('<');
        if (pos > 0) remetente = remetente[..pos].Trim().Trim('"');
        var data = r.TryGetProperty("internalDate", out var d) && long.TryParse(d.GetString(), out var ms) ? DateTimeOffset.FromUnixTimeMilliseconds(ms) : DateTimeOffset.MinValue;
        return new(r.GetProperty("id").GetString()!, WebUtility.HtmlDecode(remetente), WebUtility.HtmlDecode(Header("Subject")),
            r.TryGetProperty("snippet", out var s) ? WebUtility.HtmlDecode(s.GetString() ?? "") : "", data,
            r.TryGetProperty("labelIds", out var labels) && labels.EnumerateArray().Any(x => x.GetString() == "UNREAD"));
    }
    public async Task DesconectarAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            var refresh = _dados.RefreshToken;
            _dados = _dados with { RefreshToken = "", AccessToken = "", Expira = default }; Salvar();
            if (refresh.Length == 0) return;
            try
            {
                using var r = await _http.PostAsync("https://oauth2.googleapis.com/revoke", new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = refresh }), ct);
                r.EnsureSuccessStatusCode();
            }
            catch { throw new InvalidOperationException("A conta foi removida do Jarvis. Sem conexão para revogar no Google; remova também o acesso em myaccount.google.com/permissions."); }
        }
        finally { _gate.Release(); }
    }
}
