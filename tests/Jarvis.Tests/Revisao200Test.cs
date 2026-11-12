using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using Jarvis.Core;
using Jarvis.Server;
using Jarvis.Sistema;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
namespace Jarvis.Tests;
public sealed class Revisao200Test
{
    [Theory]
    [InlineData("Pervis, abrir calculadora.")]
    [InlineData("Carvis, abrir calculadora.")]
    [InlineData("Garvis, abriu calculadora.")]
    [InlineData("Javis, abra a calculadora.")]
    [InlineData("Jarvis, abrir calculadora.")]
    public void VariacoesDoNomeNaoImpedemComandoFixo(string texto) => Assert.Equal("abrir_app", ComandoRemotoVoz.Traduzir(texto)?.Nome);
    [Theory]
    [InlineData("não abrir calculadora")]
    [InlineData("Jarvis não abrir calculadora")]
    [InlineData("execute cmd.exe /c qualquer coisa")]
    public void NegacaoETextoLivreNaoViramComandos(string texto) => Assert.Null(ComandoRemotoVoz.Traduzir(texto));
    [Fact] public async Task VozLocalRealTranscreveAudioPiperSemChaveDeApi()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory); while (root is not null && !Directory.Exists(Path.Combine(root.FullName, "models"))) root = root.Parent;
        var folder = Path.Combine(root!.FullName, "artifacts", "voice"); Directory.CreateDirectory(folder); var file = Path.Combine(folder, "comando-mobile-200.wav");
        using var piper = new Jarvis.Voice.PiperTts(Path.Combine(root.FullName, "models")); await piper.SintetizarAsync("Jarvis, abrir calculadora.", file);
        using var reader = new NAudio.Wave.WaveFileReader(file); using var converter = new NAudio.Wave.MediaFoundationResampler(reader, new NAudio.Wave.WaveFormat(16000, 16, 1));
        using var output = new MemoryStream(); var buffer = new byte[8192]; int n; while ((n = converter.Read(buffer, 0, buffer.Length)) > 0) output.Write(buffer, 0, n);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        var texto = await new Jarvis.Desktop.Services.ControleRemotoPc().TranscreverAsync(output.ToArray(), timeout.Token);
        File.WriteAllText(Path.Combine(folder, "transcricao-mobile-200.txt"), texto);
        Assert.Contains("calculadora", ComandoRemotoVoz.Normalizar(texto)); var pedido = ComandoRemotoVoz.Traduzir(texto); Assert.Equal("abrir_app", pedido?.Nome);
        Assert.Equal("calc", ComandoRemotoVoz.EncontrarApp([new AppInfo("calc", "Calculadora", "calculadora", false, false)], pedido!.Argumento!)?.Id);
    }
    [Fact] public void AdicionarExecutavelEAtalhoPersisteNaSelecaoSemDuplicarCatalogo()
    {
        var dir = Path.Combine(Path.GetTempPath(), "Jarvis200-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
        try
        {
            var app = new AppController(dir); var exe = Path.Combine(Environment.SystemDirectory, "notepad.exe");
            app.AdicionarApp("Notas pessoais", exe, "minhas notas"); app.AdicionarApp("Notas pessoais", exe, "minhas notas");
            Assert.Single(app.ListarApps().Where(a => a.ApelidoVoz == "minhas notas"));
            Assert.Contains(new AppController(dir).ListarApps(), a => a.ApelidoVoz == "minhas notas");
            Assert.Throws<ArgumentException>(() => app.AdicionarApp("Falha", "cmd.exe /c qualquer", "falha"));
            object? shell = null, link = null;
            try { shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!); link = ((dynamic)shell!).CreateShortcut(Path.Combine(dir, "Notas.lnk")); ((dynamic)link).TargetPath = exe; ((dynamic)link).Arguments = ""; ((dynamic)link).Save(); }
            finally { if (link is not null) System.Runtime.InteropServices.Marshal.FinalReleaseComObject(link); if (shell is not null) System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell); }
            app.AdicionarApp("Atalho de notas", Path.Combine(dir, "Notas.lnk"), "atalho notas");
            Assert.Single(app.ListarApps().Where(a => a.ApelidoVoz == "atalho notas"));
        }
        finally { Directory.Delete(dir, true); }
    }
    [Fact] public void ReatorCompartilhadoAnimaEMantemIdentidadeUnica()
    {
        var anterior = TemasJarvis.Estado;
        try
        {
            byte[] Render(string tema, double t)
            {
                TemasJarvis.Escolher(tema); using var s = SKSurface.Create(new SKImageInfo(600, 600)); Jarvis.Rendering.ReatorRealista.Desenhar(s.Canvas, 600, 600, t, .1); using var im = s.Snapshot(); using var png = im.Encode(SKEncodedImageFormat.Png, 100); return png.ToArray();
            }
            var a = Render("azul", 0); var b = Render("azul", 4); var c = Render("ambar", 4); Assert.False(a.SequenceEqual(b)); Assert.True(b.SequenceEqual(c));
            var root = new DirectoryInfo(AppContext.BaseDirectory); while (root is not null && !File.Exists(Path.Combine(root.FullName, "Jarvis.sln"))) root = root.Parent;
            var outDir = Path.Combine(root!.FullName, "artifacts", "preview"); Directory.CreateDirectory(outDir);
            File.WriteAllBytes(Path.Combine(outDir, "Reator200-ciano.png"), b); File.WriteAllBytes(Path.Combine(outDir, "Reator200-ambar.png"), c);
            Assert.Throws<ArgumentException>(() => TemasJarvis.Escolher("tema-invalido"));
        }
        finally { TemasJarvis.Sincronizar(anterior); }
    }
    private sealed class Pc : IControleRemoto
    {
        public int Comandos, Transcricoes;
        public IReadOnlyList<AppInfo> Apps() => [];
        public Task<string> ExecutarAsync(string nome, string? arg, bool ok) { Comandos++; return Task.FromResult("ok"); }
        public Task<string> TranscreverAsync(byte[] pcm, CancellationToken ct) { Transcricoes++; Assert.Equal(32000, pcm.Length); return Task.FromResult("Jarvis, abrir calculadora"); }
    }
    private static int Port() { using var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); return ((IPEndPoint)listener.LocalEndpoint).Port; }
    [Fact] public async Task VozETemaExigemTokenETlsEAudioNaoExecutaComando()
    {
        var anterior = TemasJarvis.Estado; using var cert = CertificadoLocal.Gerar("127.0.0.1"); var pc = new Pc(); var plain = Port(); var tls = Port();
        await using var app = HospedeJarvis.Criar([], plain, tls, cert, pc); await app.StartAsync(); var pares = app.Services.GetRequiredService<Aparelhos>(); var token = pares.Parear("Teste revisão 200 temporário");
        try
        {
            using var http = new HttpClient(); Assert.Equal(HttpStatusCode.Forbidden, (await http.PostAsJsonAsync($"http://127.0.0.1:{plain}/api/tema", new EstadoTema("ambar", 0))).StatusCode);
            using var https = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, c, _, _) => c?.Thumbprint == cert.Thumbprint }) { BaseAddress = new($"https://127.0.0.1:{tls}") };
            Assert.Equal(HttpStatusCode.Unauthorized, (await https.GetAsync("/api/tema")).StatusCode);
            using var semToken = new ByteArrayContent(new byte[32000]); semToken.Headers.ContentType = new("audio/pcm"); Assert.Equal(HttpStatusCode.Unauthorized, (await https.PostAsync("/api/voz/transcrever", semToken)).StatusCode);
            https.DefaultRequestHeaders.Add("X-Jarvis-Token", token);
            using var pcm = new ByteArrayContent(new byte[32000]); pcm.Headers.ContentType = new("audio/pcm"); var voz = await https.PostAsync("/api/voz/transcrever", pcm); voz.EnsureSuccessStatusCode(); Assert.Contains("calculadora", await voz.Content.ReadAsStringAsync()); Assert.Equal(1, pc.Transcricoes); Assert.Equal(0, pc.Comandos);
            using var curto = new ByteArrayContent(new byte[17]); curto.Headers.ContentType = new("audio/pcm"); Assert.Equal(HttpStatusCode.BadRequest, (await https.PostAsync("/api/voz/transcrever", curto)).StatusCode);
            using var grande = new ByteArrayContent(new byte[480002]); grande.Headers.ContentType = new("audio/pcm"); Assert.Equal((HttpStatusCode)413, (await https.PostAsync("/api/voz/transcrever", grande)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await https.PostAsJsonAsync("/api/tema", new EstadoTema("invalido", 0))).StatusCode);
            (await https.PostAsJsonAsync("/api/tema", new EstadoTema("ambar", 0))).EnsureSuccessStatusCode(); var tema = await https.GetFromJsonAsync<EstadoTema>("/api/tema"); Assert.Equal("jarvis", tema?.Id); Assert.Equal(0, tema?.Revisao);
        }
        finally { TemasJarvis.Sincronizar(anterior); pares.Remover(token); await app.StopAsync(); }
    }
}
