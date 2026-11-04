using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
// JARVIS.Server — monta o app web para rodar embutido no Desktop ou standalone.
using System.Collections.Concurrent;

namespace Jarvis.Server;

/// <summary>Constrói o WebApplication do Jarvis (Kestrel + SignalR + pareamento).</summary>
public static class HospedeJarvis
{
    public const int PortaHttp = 5188;   // só diagnóstico (/ping), sem dado sensível
    public const int PortaHttps = 5189;  // dados e comandos, com token

    public static WebApplication Criar(string[] args, int portaHttp = PortaHttp,
        int portaHttps = PortaHttps,
        System.Security.Cryptography.X509Certificates.X509Certificate2? cert = null, IControleRemoto? controle = null, string? apk = null, CentralAparelhos? central = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSignalR();
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            options.AddPolicy("quadro-celular", ctx => RateLimitPartition.GetFixedWindowLimiter(ctx.Request.Headers["X-Jarvis-Token"].ToString(), _ => new FixedWindowRateLimiterOptions { PermitLimit = 12, Window = TimeSpan.FromSeconds(1), QueueLimit = 0 }));
            options.AddPolicy("parear", ctx => RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions
                { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            options.AddPolicy("entrada", ctx => RateLimitPartition.GetFixedWindowLimiter(
                ctx.Request.Headers["X-Jarvis-Token"].ToString(), _ => new FixedWindowRateLimiterOptions
                { PermitLimit = 30, Window = TimeSpan.FromSeconds(1), QueueLimit = 0 }));
            options.AddPolicy("voz", ctx => RateLimitPartition.GetFixedWindowLimiter(ctx.Request.Headers["X-Jarvis-Token"].ToString(), _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
        builder.Services.AddSingleton<IControleRemoto>(controle ?? new ControleRemotoIndisponivel());
        builder.Services.AddSingleton<PareamentoService>();
        builder.Services.AddSingleton<Aparelhos>();
        builder.Services.AddSingleton(central ?? new CentralAparelhos());

        builder.WebHost.ConfigureKestrel(o =>
        {
            o.ListenAnyIP(portaHttp);
            if (cert is not null)
                o.ListenAnyIP(portaHttps, lo => lo.UseHttps(cert));
        });

        var app = builder.Build();
        app.UseRateLimiter();
        app.UseWebSockets();
        var pareamento = app.Services.GetRequiredService<PareamentoService>();
        var aparelhos = app.Services.GetRequiredService<Aparelhos>();

        app.MapGet("/health", () => Results.Ok(new { app = "jarvis", versao = InstaladorMobile.Versao }));

        // Diagnóstico de rede: SEM dado sensível. O celular testa com o navegador.
        app.MapGet("/ping", () => Results.Ok(new { app = "Jarvis", versao = InstaladorMobile.Versao }));

        app.Use(async (ctx, next) =>
        {
            var path = (ctx.Request.Path.Value ?? "").ToLowerInvariant();
            if (path.StartsWith("/api/") || path.StartsWith("/hub/"))
            {
                if (!ctx.Request.IsHttps) { ctx.Response.StatusCode = 403; return; }
            }
            var ipRemoto = ctx.Connection.RemoteIpAddress;
            var local = ipRemoto is not null &&
                (System.Net.IPAddress.IsLoopback(ipRemoto.MapToIPv4()) || System.Net.IPAddress.IsLoopback(ipRemoto));
            if (path == "/mobile" || path == "/mobile/manifesto" || path == "/jarvis.apk" || path.StartsWith("/health") || path.StartsWith("/ping") || (local && path.StartsWith("/api/parear/iniciar")) || path.StartsWith("/api/parear/confirmar"))
            {
                await next();
                return;
            }
            if (!aparelhos.Validar(ctx.Request.Headers["X-Jarvis-Token"].ToString(), out var nomeCelular))
            {
                ctx.Response.StatusCode = 401;
                return;
            }
            PresencaRemota.Registrar(nomeCelular, ctx.Request.Headers["X-Jarvis-Bateria"], ctx.Request.Headers["X-Jarvis-Conexao"]);
            await next();
        });

        app.MapGet("/api/parear/iniciar", (HttpRequest req) =>
        {
            var remoto = req.HttpContext.Connection.RemoteIpAddress;
            var ehLocal = (remoto is not null &&
                (System.Net.IPAddress.IsLoopback(remoto.MapToIPv4()) || System.Net.IPAddress.IsLoopback(remoto)));
            if (!ehLocal)
                return Results.StatusCode(403);
            pareamento.EmitirPar(out var codigo, out var token);
            var ips = AparelhosIp.Candidatos()
                .Select(x => new { ip = x.Ip, nome = x.Nome, cabeado = x.Cabeado }).ToList();
            var ip = ips.Count > 0 ? ips[0].ip : "127.0.0.1";
            var parear = PareamentoService.MontarQr(ip, portaHttps, token, CertificadoCache.Impressao);
            parear += "&ips=" + Uri.EscapeDataString(string.Join(",", ips.Select(x => x.ip).Where(a => !Jarvis.Core.EnderecosJarvis.Tailscale("https://" + a + ":5189")).Concat(Jarvis.Core.RedeDireta.PublicosDoPc().Select(a => new Uri(a).Host.Trim('[', ']'))).Take(8)));
            var qr = parear;
            return Results.Ok(new { codigo, qrTexto = qr, qrPng = Convert.ToBase64String(PareamentoService.QrPng(qr)), ips, porta = portaHttps, ip });
        });

        app.MapPost("/api/parear/confirmar", (ParearReq corpo) =>
        {
            if (!pareamento.ValidarToken(corpo.Token)) return Results.Unauthorized();
            var identity = IdentidadeRede.Obter();
            return Results.Ok(new { token = aparelhos.Parear(corpo.Nome ?? "celular"), enderecos = string.Join("|", identity.Enderecos ?? []), remoto = identity.EnderecoRemoto ?? "" });
        }).RequireRateLimiting("parear");

        app.MapPost("/api/parear/confirmar-codigo", (ParearCodigoReq corpo, HttpContext ctx) =>
        {
            if (ctx.Connection.RemoteIpAddress is not { } source || !Jarvis.Core.RedeDireta.MesmaRede(source)) return Results.StatusCode(403);
            if (!pareamento.ValidarCodigo(corpo.Codigo, out var token)) return Results.Unauthorized();
            var identity = IdentidadeRede.Obter();
            return Results.Ok(new { token = aparelhos.Parear(corpo.Nome ?? "celular"), impressao = CertificadoCache.Impressao, enderecos = string.Join("|", identity.Enderecos ?? []), remoto = identity.EnderecoRemoto ?? "" });
        }).RequireRateLimiting("parear");

        app.MapGet("/api/apps", (IControleRemoto pc) => Results.Ok(pc.Apps()));
        app.MapGet("/api/tema", () => Results.Ok(Jarvis.Core.TemasJarvis.Estado));
        app.MapPost("/api/tema", (Jarvis.Core.EstadoTema tema) => { try { return Results.Ok(Jarvis.Core.TemasJarvis.Escolher(tema.Id)); } catch (ArgumentException e) { return Results.BadRequest(new { mensagem = e.Message }); } });
        app.MapPost("/api/voz/transcrever", async (HttpContext ctx, IControleRemoto pc) =>
        {
            if (ctx.Request.ContentType != "audio/pcm") return Results.BadRequest(new { mensagem = "Use PCM16 mono de 16 kHz." });
            using var output = new MemoryStream(); var chunk = new byte[16384]; int n;
            while ((n = await ctx.Request.Body.ReadAsync(chunk, ctx.RequestAborted)) > 0)
            { if (output.Length + n > 480000) return Results.StatusCode(413); output.Write(chunk, 0, n); }
            var pcm = output.ToArray();
            if (pcm.Length < 6400 || pcm.Length % 2 != 0) return Results.BadRequest(new { mensagem = "Áudio curto ou incompleto. Tente falar novamente." });
            try { return Results.Ok(new { texto = await pc.TranscreverAsync(pcm, ctx.RequestAborted) }); }
            catch (InvalidOperationException e) { return Results.Json(new { mensagem = e.Message }, statusCode: 503); }
        }).RequireRateLimiting("voz");
        RotasAparelho.Mapear(app);
        app.MapGet("/api/agenda", (IControleRemoto pc) => Results.Ok(pc.Agenda()));
        app.MapPost("/api/agenda", (Jarvis.Core.Compromisso item, IControleRemoto pc) =>
        {
            try { Jarvis.Core.AgendaDatas.Validar(item, DateTime.Today); return Results.Ok(pc.SalvarCompromisso(item)); }
            catch (Jarvis.Core.ConflitoAgendaException ex) { return Results.Conflict(new { mensagem = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { mensagem = ex.Message }); }
        });
        app.MapGet("/api/midia", async (IControleRemoto pc) => Results.Ok(await pc.MidiaAsync()));
        app.MapGet("/api/apps/disponiveis", (IControleRemoto pc) => Results.Ok(pc.AppsDisponiveis()));
        app.MapPost("/api/apps/selecao", (SelecaoApps selecao, IControleRemoto pc) =>
        {
            if (selecao.Ids is null || selecao.Ids.Length > 500) return Results.BadRequest(new { mensagem = "Seleção inválida." });
            try { pc.SelecionarApps(selecao.Ids); return Results.Ok(new { mensagem = "Seus aplicativos foram atualizados." }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { mensagem = ex.Message }); }
        });
        app.MapGet("/api/identidade", (HttpContext ctx) => Results.Ok(IdentidadeRede.Obter(ctx.Connection.LocalIpAddress?.MapToIPv4().ToString())));
        app.MapGet("/api/telemetria", async (IControleRemoto pc) => Results.Ok(await pc.TelemetriaAsync()));
        app.MapGet("/api/janelas", (IControleRemoto pc) => Results.Ok(pc.Janelas()));
        app.MapGet("/api/monitores", (IControleRemoto pc) => Results.Ok(pc.Monitores()));
        app.MapPost("/api/janelas/layout", async (PedidoOrganizacao corpo, IControleRemoto pc) =>
        {
            if (corpo.Layout is null) return Results.BadRequest(new { mensagem = "Escolha um layout." });
            try { return Results.Ok(await pc.OrganizarAsync(corpo.Layout, corpo.Confirmado)); }
            catch (ArgumentException ex) { return Results.BadRequest(new { mensagem = ex.Message }); }
        });
        app.MapGet("/api/tela/stream", TelaStream.Executar);
        app.MapGet("/api/tela/entrada", (IControleRemoto pc) => Results.Ok(pc.Entrada()));
        app.MapPost("/api/tela/entrada", async (Jarvis.Core.PedidoToque pedido, IControleRemoto pc, HttpContext ctx) =>
        {
            try { pedido.Validar(); await pc.ToqueAsync(pedido, ctx.RequestAborted); return Results.Ok(); }
            catch (ArgumentException ex) { return Results.BadRequest(new { mensagem = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.Json(new { mensagem = ex.Message }, statusCode: 423); }
        }).RequireRateLimiting("entrada");
        app.MapGet("/api/tela", (HttpContext ctx, IControleRemoto pc) =>
        {
            ctx.Response.Headers.CacheControl = "no-store, no-cache";
            ctx.Response.Headers.Pragma = "no-cache";
            try
            {
                var frame = pc.Tela(ctx.Request.Query["monitor"].FirstOrDefault());
                ctx.Response.Headers["X-Jarvis-Instante"] = frame.Instante.ToString("O");
                return Results.File(frame.Jpeg, "image/jpeg");
            }
            catch (InvalidOperationException ex) { return Results.Json(new { mensagem = ex.Message }, statusCode: 423); }
            catch (Exception) { return Results.Json(new { mensagem = "Captura temporariamente indisponível." }, statusCode: 503); }
        });
        app.MapPost("/api/comando", async (PedidoComando pedido, IControleRemoto pc) =>
        {
            if (!ComandosFixos.Permitidos.Contains(pedido.Nome)) return Results.BadRequest(new { mensagem = "Comando não permitido." });
            try { return Results.Ok(new { mensagem = await pc.ExecutarAsync(pedido.Nome, pedido.Argumento, pedido.Confirmado) }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { mensagem = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { mensagem = ex.Message }); }
            catch (System.ComponentModel.Win32Exception) { return Results.BadRequest(new { mensagem = "O Windows recusou esta ação. O aplicativo pode estar elevado ou indisponível." }); }
        });
        InstaladorMobile.Mapear(app, apk);
        app.MapHub<JarvisHub>("/hub/jarvis");

        // Endpoints já configurados no Kestrel acima (ListenAnyIP/UseUrls).
        return app;
    }
}

public sealed record ParearReq(string Token, string? Nome);
public sealed record SelecaoApps(string[] Ids);
public sealed record PedidoOrganizacao(Jarvis.Core.PedidoLayout Layout, bool Confirmado = false);
public sealed record ParearCodigoReq(string Codigo, string? Nome);

/// <summary>Aparelhos pareados (token duradouro → nome), persistido local.</summary>
public sealed class Aparelhos
{
    private readonly ConcurrentDictionary<string, string> _mapa = new();
    private readonly string _arquivo;
    public Aparelhos()
    {
        var pasta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Jarvis");
        Directory.CreateDirectory(pasta);
        _arquivo = Path.Combine(pasta, "aparelhos.json");
        try
        {
            foreach (var kv in System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(_arquivo)) ?? new())
                _mapa[kv.Key] = kv.Value;
        }
        catch { }
    }
    public string Parear(string nome)
    {
        var t = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24));
        _mapa[t] = nome;
        Salvar();
        return t;
    }
    public bool Validar(string token, out string nome) => _mapa.TryGetValue(token, out nome!);
    public void Remover(string token) { _mapa.TryRemove(token, out _); Salvar(); }
    private void Salvar()
    {
        try { File.WriteAllText(_arquivo, System.Text.Json.JsonSerializer.Serialize(_mapa)); } catch { }
    }
}

public static class AparelhosIp
{
    private static readonly string[] Virtuais = { "wsl", "hyper-v", "virtualbox", "virtual", "vpn", "tailscale", "vmware", "loopback", "pseudo", "teredo", "isatap" };

    /// <summary>Lista de IPs candidatos: Ethernet com gateway primeiro, virtuais por último.</summary>
    public static List<(string Ip, string Nome, bool Cabeado)> Candidatos()
    {
        var lista = new List<(string Ip, string Nome, bool Cabeado, int Prioridade)>();
        try
        {
            foreach (var iface in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (iface.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
                var desc = (iface.Description + " " + iface.Name).ToLowerInvariant();
                if (desc.Contains("loopback")) continue;
                var virtual_ = Virtuais.Any(v => desc.Contains(v));
                var cabeado = iface.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Ethernet && !virtual_;
                var temGateway = iface.GetIPProperties().GatewayAddresses.Any(g =>
                    !g.Address.ToString().StartsWith("fe80", StringComparison.OrdinalIgnoreCase));
                foreach (var addr in iface.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) continue;
                    var str = addr.Address.ToString();
                    if (str.StartsWith("127.") || str.StartsWith("169.254.")) continue;
                    lista.Add((str, iface.Name, cabeado && temGateway, virtual_ ? 2 : temGateway ? 0 : 1));
                }
            }
        }
        catch { }
        return lista.OrderBy(x => x.Prioridade).ThenByDescending(x => x.Cabeado).ThenBy(x => x.Ip).Select(x => (x.Ip, x.Nome, x.Cabeado)).ToList();
    }

    public static string Local()
    {
        var c = Candidatos();
        if (c.Count > 0) return c[0].Ip;
        try
        {
            using var s = new System.Net.Sockets.Socket(
                System.Net.Sockets.AddressFamily.InterNetwork,
                System.Net.Sockets.SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
            s.Connect("8.8.8.8", 65530);
            return (s.LocalEndPoint as System.Net.IPEndPoint)?.Address.ToString() ?? "127.0.0.1";
        }
        catch { return "127.0.0.1"; }
    }
}

public static class CertificadoCache
{
    public static string Impressao { get; set; } = "";
}
