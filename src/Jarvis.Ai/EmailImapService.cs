using System.Security.Cryptography;
using System.Text.Json;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using Jarvis.Core;
namespace Jarvis.Ai;

/// <summary>Gmail por IMAP/TLS com senha de app. Sem Gmail API, Cloud Console ou envio de mensagens.</summary>
public sealed class EmailImapService : IEmailService
{
    private sealed record Credencial(string Email, string Senha);
    private readonly string _arquivo;
    private readonly SemaphoreSlim _gate = new(1);
    private Credencial? _conta;
    private readonly IEmailService _oauth;
    public EmailImapService(string? pasta = null, IEmailService? oauth = null) {
        pasta ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis"); Directory.CreateDirectory(pasta);
        _arquivo = Path.Combine(pasta, "gmail-imap.dpapi"); _oauth = oauth ?? new GoogleMailService(pasta);
        byte[]? bytes = null;
        try { bytes = ProtectedData.Unprotect(File.ReadAllBytes(_arquivo), null, DataProtectionScope.CurrentUser); _conta = JsonSerializer.Deserialize<Credencial>(bytes); } catch { }
        finally { if (bytes is not null) CryptographicOperations.ZeroMemory(bytes); }
    }
    public bool Configurado => _conta is not null || _oauth.Configurado;
    public bool Conectado => _conta is not null || _oauth.Conectado;
    public static string NormalizarSenha(string senha) {
        var result = string.Concat(senha.Where(c => !char.IsWhiteSpace(c)));
        if (result.Length != 16 || !result.All(char.IsAsciiLetter)) throw new ArgumentException("Use a senha de app de 16 letras criada na conta Google. A senha normal da conta não funciona.");
        return result;
    }
    private static async Task<ImapClient> AbrirAsync(Credencial conta, CancellationToken ct) {
        var client = new ImapClient { Timeout = 20000 };
        try {
            await client.ConnectAsync("imap.gmail.com", 993, SecureSocketOptions.SslOnConnect, ct).ConfigureAwait(false);
            client.AuthenticationMechanisms.Remove("XOAUTH2");
            await client.AuthenticateAsync(conta.Email, conta.Senha, ct).ConfigureAwait(false); return client;
        } catch { client.Dispose(); throw; }
    }
    public async Task ConectarSenhaAppAsync(string email, string senhaApp, CancellationToken ct = default) {
        email = email.Trim();
        if (!System.Net.Mail.MailAddress.TryCreate(email, out var address) || address.Address != email) throw new ArgumentException("Informe seu endereço de e-mail completo.");
        var conta = new Credencial(email, NormalizarSenha(senhaApp));
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try {
            using var client = await AbrirAsync(conta, ct).ConfigureAwait(false);
            await client.Inbox.OpenAsync(FolderAccess.ReadOnly, ct).ConfigureAwait(false);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(conta);
            try { File.WriteAllBytes(_arquivo + ".tmp", ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser)); File.Move(_arquivo + ".tmp", _arquivo, true); }
            finally { CryptographicOperations.ZeroMemory(bytes); }
            _conta = conta; await client.DisconnectAsync(true, ct).ConfigureAwait(false);
        } catch (MailKit.Security.AuthenticationException) { throw new InvalidOperationException("O Gmail recusou a conexão. Confira o e-mail, ative a verificação em duas etapas e gere uma nova senha de app. Contas administradas podem bloquear esse recurso."); }
        finally { _gate.Release(); }
    }
    public Task ConfigurarClienteAsync(string arquivoJson) => _oauth.ConfigurarClienteAsync(arquivoJson);
    public Task ConectarAsync(CancellationToken ct = default) => _conta is null ? _oauth.ConectarAsync(ct) : Task.CompletedTask;
    public async Task<CaixaEntrada> SincronizarAsync(CancellationToken ct = default) {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try {
            if (_conta is null) return await _oauth.SincronizarAsync(ct).ConfigureAwait(false);
            using var client = await AbrirAsync(_conta, ct).ConfigureAwait(false);
            var inbox = client.Inbox; await inbox.OpenAsync(FolderAccess.ReadOnly, ct).ConfigureAwait(false);
            var items = inbox.Count == 0 ? [] : await inbox.FetchAsync(Math.Max(0, inbox.Count - 20), -1, MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope | MessageSummaryItems.Flags | MessageSummaryItems.PreviewText, ct).ConfigureAwait(false);
            var messages = items.Select(m => new EmailResumo(m.UniqueId.ToString(), m.Envelope?.From?.ToString() ?? "", m.Envelope?.Subject ?? "(Sem assunto)", m.PreviewText ?? "", m.Envelope?.Date ?? DateTimeOffset.MinValue, m.Flags?.HasFlag(MessageFlags.Seen) != true)).OrderByDescending(m => m.Data).ToArray();
            var unread = await inbox.SearchAsync(MailKit.Search.SearchQuery.NotSeen, ct).ConfigureAwait(false);
            var result = new CaixaEntrada(messages, inbox.Count, unread.Count);
            await client.DisconnectAsync(true, ct).ConfigureAwait(false); return result;
        } finally { _gate.Release(); }
    }
    public async Task DesconectarAsync(CancellationToken ct = default) {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try { _conta = null; if (File.Exists(_arquivo)) File.Delete(_arquivo); if (_oauth.Conectado) await _oauth.DesconectarAsync(ct).ConfigureAwait(false); }
        finally { _gate.Release(); }
    }
}
