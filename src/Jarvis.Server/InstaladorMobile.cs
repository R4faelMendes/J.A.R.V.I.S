using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Net.Http.Headers;
namespace Jarvis.Server;

public static class InstaladorMobile
{
    public const string Versao = "2.3.0";
    private static readonly ConcurrentDictionary<string, (long Size, long Modified, string Hash)> Hashes = new();
    public sealed record Pacote(string Arquitetura, long Bytes, string Sha256, string Url);
    private static string? Arquivo(string? apk, string arquitetura)
    {
        if (apk is null) return null;
        var file = apk;
        return File.Exists(file) ? file : null;
    }
    private static Pacote LerPacote(string file, string arquitetura)
    {
        var info = new FileInfo(file);
        if (!Hashes.TryGetValue(file, out var cached) || cached.Size != info.Length || cached.Modified != info.LastWriteTimeUtc.Ticks)
        {
            using var stream = File.OpenRead(file);
            cached = (info.Length, info.LastWriteTimeUtc.Ticks, Convert.ToHexString(SHA256.HashData(stream))); Hashes[file] = cached;
        }
        return new(arquitetura, cached.Size, cached.Hash, "/jarvis.apk?arquitetura=" + arquitetura);
    }
    public static void Mapear(WebApplication app, string? apk)
    {
        app.MapMethods("/jarvis.apk", ["GET", "HEAD"], (HttpContext context) =>
        {
            var architecture = context.Request.Query["arquitetura"].ToString();
            if (architecture is not ("" or "universal")) return Results.BadRequest("Use o instalador universal, compatível com Android de 32 e 64 bits.");
            architecture = "universal";
            var file = Arquivo(apk, architecture); if (file is null) return Results.NotFound("Instalador não disponível. Atualize o Jarvis no PC ou escolha a versão universal.");
            var package = LerPacote(file, architecture);
            context.Response.Headers.CacheControl = "private, max-age=0, must-revalidate";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            return Results.File(file, "application/vnd.android.package-archive", $"Jarvis-{Versao}-{architecture}.apk", new FileInfo(file).LastWriteTimeUtc,
                new EntityTagHeaderValue('"' + package.Sha256 + '"'), enableRangeProcessing: true);
        });
        app.MapGet("/mobile/manifesto", () => Results.Ok(new
        {
            versao = Versao,
            pacotes = new[] { "universal" }.Select(a => (a, path: Arquivo(apk, a))).Where(p => p.path is not null).Select(p => LerPacote(p.path!, p.a)).ToArray()
        }));
        app.MapGet("/mobile", (HttpContext ctx) =>
        {
            ctx.Response.Headers["Referrer-Policy"] = "no-referrer"; ctx.Response.Headers["Cache-Control"] = "no-store";
            ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
            return Results.Content(Pagina, "text/html; charset=utf-8");
        });
    }
    private const string Pagina = """
<!doctype html><html lang="pt-BR"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Instalar Jarvis no Android</title>
<style>body{background:#172431;color:#e6f6ff;font:16px system-ui;max-width:560px;margin:20px auto;padding:22px}h1{letter-spacing:7px}small{color:#8fa9bf}section{background:#1d2d3c;border:1px solid #1b3a57;border-radius:5px;padding:24px;margin:20px 0}a{display:block;background:#82e0e6;color:#050d18;padding:18px;border-radius:4px;text-decoration:none;text-align:center;font-weight:650;margin-top:16px}a.secondary{background:transparent;color:#b1d7eb;border:1px solid #31536a;font-size:14px}p{line-height:1.55}li{margin:12px 0}[hidden]{display:none!important}code{overflow-wrap:anywhere;font-size:11px;color:#8fa9bf}details{margin-top:20px}#tamanho{color:#6ae6c3}</style>
<small>APLICATIVO ANDROID · SEM CABO USB</small><h1>JARVIS</h1>
<section><h2 id="titulo">Jarvis 2.3.0</h2><p>Instale por cima da versão atual para manter o pareamento e os dados. Inclui o reator frontal e o widget para sua tela inicial.</p>

<a id="universal" href="/jarvis.apk">Baixar Jarvis · versão universal</a>
<p><small>Um único instalador para Android de 32 e 64 bits, com o ícone Jarvis. Esta versão não solicita Acessibilidade nem leitura de dados de outros apps. O controle do celular pelo PC não está incluído; o controle do PC pelo celular continua disponível.</small></p>
<ol><li>Baixe pelo Wi-Fi da casa para evitar a velocidade da internet.</li><li>Abra o APK em Downloads e confirme a atualização.</li><li>Abra Jarvis. Ele procura seu PC no mesmo Wi-Fi; confirme o PIN apenas no primeiro pareamento.</li></ol>
<p><small>A assinatura de atualização é mantida. O Android/Google Play Protect pode verificar o APK por ele ser instalado fora da loja. Mantenha essa proteção ativa.</small></p>
<details><summary>Verificar integridade do download</summary><p id="integridade">Carregando tamanhos e SHA-256…</p><p><small>O download aceita retomada. O Android também verifica a assinatura do pacote ao atualizar.</small></p></details></section>
<section><h2>Abrir o aplicativo</h2><p>O navegador é usado apenas para baixar. O Jarvis funciona como aplicativo instalado.</p><a id="parear" hidden>Abrir Jarvis e parear</a></section>
<script>
const mb=n=>(n/1048576).toFixed(1)+' MB';
fetch('/mobile/manifesto',{cache:'no-store'}).then(r=>r.json()).then(m=>{
document.getElementById('titulo').textContent='Jarvis '+m.versao;
const universal=m.pacotes.find(p=>p.arquitetura==='universal');
if(universal)document.getElementById('universal').textContent='Baixar Jarvis · versão universal · '+mb(universal.bytes);
const area=document.getElementById('integridade');area.replaceChildren();for(const p of m.pacotes){const line=document.createElement('p');line.textContent=p.arquitetura+' · '+mb(p.bytes);const hash=document.createElement('code');hash.textContent=p.sha256;line.append(document.createElement('br'),hash);area.append(line);}
}).catch(()=>document.getElementById('integridade').textContent='Não foi possível consultar a integridade. Atualize esta página.');
try{const link=decodeURIComponent(location.hash.slice(1));if(link.startsWith('jarvis://parear?')){const a=document.getElementById('parear');a.href=link;a.hidden=false}}catch{}
</script></html>
""";
}
