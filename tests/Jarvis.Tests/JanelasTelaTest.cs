using Jarvis.Core;
using Jarvis.Server;
using Jarvis.Sistema;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
namespace Jarvis.Tests;

public sealed class JanelasTelaTest
{
    [Theory]
    [InlineData("Metade", 2)] [InlineData("TresColunas", 3)] [InlineData("FocoLateral", 2)] [InlineData("Grade2x2", 4)]
    public void GradeRespeitaMonitorNegativoBarraETamanhoImpar(string modelo, int quantidade)
    {
        var area = new RegiaoJanela(-1919, -97, 1919, 1041);
        var cells = GradeJanelas.Calcular(modelo, area, quantidade);
        Assert.Equal(quantidade, cells.Count);
        foreach (var c in cells)
        { Assert.InRange(c.X, area.X, area.X + area.Largura); Assert.InRange(c.Y, area.Y, area.Y + area.Altura); Assert.True(c.X + c.Largura <= area.X + area.Largura); Assert.True(c.Y + c.Altura <= area.Y + area.Altura); }
        for (var i = 0; i < cells.Count; i++) for (var j = i + 1; j < cells.Count; j++)
        { var a = cells[i]; var b = cells[j]; Assert.False(a.X < b.X + b.Largura && a.X + a.Largura > b.X && a.Y < b.Y + b.Altura && a.Y + a.Altura > b.Y); }
    }
    [Fact]
    public void GradeNaoDescartaJanelasSilenciosamente()
    {
        Assert.Throws<ArgumentException>(() => GradeJanelas.Calcular("Metade", new(0, 0, 1920, 1040), 3));
        Assert.Throws<ArgumentException>(() => GradeJanelas.Calcular("Grade2x2", new(0, 0, 1920, 1040), 0));
        Assert.Throws<ArgumentException>(() => GradeJanelas.Calcular("livre", new(0, 0, 1920, 1040), 1));
    }
    [Fact]
    public void ReatorTemMovimentoDeOrbitasEFilamentos()
    {
        var a = ReactorFluxo.Gerar(0, 0); var b = ReactorFluxo.Gerar(1, .7);
        Assert.Equal(10, a.Count);
        Assert.NotEqual(a[0].Pontos[0], b[0].Pontos[0]);
        Assert.All(b.SelectMany(t => t.Pontos), p => { Assert.InRange(p.X, 0, 1); Assert.InRange(p.Y, 0, 1); });
        Assert.NotEqual(a[^1].Pontos[5], b[^1].Pontos[5]);
    }
    [Fact]
    public void TransmissaoDesativadaNaoCaptura()
    {
        var tela = new TransmissaoTela(); Assert.True(tela.Permitida); tela.Permitida = false;
        Assert.Throws<InvalidOperationException>(() => tela.Capturar()); Assert.Null(tela.UltimaVisualizacao);
    }
    [System.Runtime.InteropServices.DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(nint h, int a, out NativeRect r, int size);
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    private static void ProcessarMensagens()
    {
        var frame = new System.Windows.Threading.DispatcherFrame();
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; }; timer.Start(); System.Windows.Threading.Dispatcher.PushFrame(frame);
    }
    [Fact]
    public void JanelasReaisSelecionadasEJpegDaTela()
    {
        Exception? erro = null;
        var thread = new Thread(() =>
        {
            System.Windows.Window? a = null, b = null;
            try
            {
                a = new() { Title = "Teste Jarvis · janela A", Width = 360, Height = 260, MinWidth = 100, MinHeight = 100 };
                b = new() { Title = "Teste Jarvis · janela B", Width = 360, Height = 260, MinWidth = 100, MinHeight = 100 };
                a.Show(); b.Show(); ProcessarMensagens();
                var ha = new System.Windows.Interop.WindowInteropHelper(a).Handle; var hb = new System.Windows.Interop.WindowInteropHelper(b).Handle;
                var controller = new WindowController(); var monitor = controller.ListarMonitores().First(m => m.Principal);
                var result = controller.Organizar(new("Metade", monitor.Id, new[] { (long)hb, (long)ha }));
                ProcessarMensagens(); Assert.Equal(2, result.Movidas);
                Assert.Equal(0, DwmGetWindowAttribute(hb, 9, out var left, 16)); Assert.Equal(0, DwmGetWindowAttribute(ha, 9, out var right, 16));
                Assert.True(left.Right <= right.Left + 2, "As duas janelas de teste não devem se sobrepor.");
                Assert.InRange(left.Left, monitor.X, monitor.X + 24); Assert.True(right.Right <= monitor.X + monitor.Largura + 4);
                Assert.Throws<ArgumentException>(() => controller.Organizar(new("Metade", monitor.Id, new[] { (long)ha, (long)ha })));
                var corner = controller.Organizar(new("Grade2x2", monitor.Id, new[] { (long)ha }, new[] { 3 })); Assert.Equal(1, corner.Movidas);
                ProcessarMensagens(); Assert.Equal(0, DwmGetWindowAttribute(ha, 9, out var bottomRight, 16));
                Assert.True(bottomRight.Left >= monitor.X + monitor.Largura / 2); Assert.True(bottomRight.Top >= monitor.Y + monitor.Altura / 2);
                var tela = new TransmissaoTela { Permitida = true };
                try { var jpeg = tela.Capturar(monitor.Id); Assert.InRange(jpeg.Largura, 1, 1280); Assert.InRange(jpeg.Altura, 1, 720); Assert.Equal((byte)255, jpeg.Jpeg[0]); Assert.Equal((byte)216, jpeg.Jpeg[1]); Assert.Same(jpeg, tela.Capturar(monitor.Id)); foreach (var outro in controller.ListarMonitores().Where(m => m.Id != monitor.Id)) { var secundaria = tela.Capturar(outro.Id); Assert.NotSame(jpeg, secundaria); Assert.Equal((byte)216, secundaria.Jpeg[1]); } Assert.Throws<InvalidOperationException>(() => tela.Capturar("monitor-inexistente")); }
                finally { tela.Permitida = false; }
                controller.Fechar(hb); ProcessarMensagens(); Assert.DoesNotContain(controller.ListarJanelas(), j => j.Handle == hb);
                Assert.Throws<ArgumentException>(() => controller.Organizar(new("Metade", monitor.Id, new[] { (long)hb })));
            }
            catch (Exception ex) { erro = ex; }
            finally { a?.Close(); b?.Close(); }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(20))); Assert.Null(erro);
    }
    private sealed class PcTela : IControleRemoto
    {
        public bool Ativa; public PedidoLayout? Recebido;
        public string? MonitorRecebido;
        public IReadOnlyList<AppInfo> Apps() => Array.Empty<AppInfo>();
        public Task<string> ExecutarAsync(string comando, string? id, bool confirmado) => Task.FromResult("ok");
        public IReadOnlyList<MonitorInfo> Monitores() => new[] { new MonitorInfo("principal", "Principal", 0, 0, 1920, 1040, true) };
        public Task<ResultadoLayout> OrganizarAsync(PedidoLayout pedido, bool confirmado) { GradeJanelas.Capacidade(pedido.Modelo); Recebido = pedido; return Task.FromResult(new ResultadoLayout(pedido.Janelas?.Length ?? 0, Array.Empty<string>())); }
        public QuadroTela Tela() => Ativa ? new(new byte[] { 255, 216, 255, 217 }, 1, 1, DateTimeOffset.UtcNow) : throw new InvalidOperationException("Compartilhamento desativado.");
        public QuadroTela Tela(string? id) { MonitorRecebido = id; return Tela(); }
    }
    private static int Porta() { using var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); return ((IPEndPoint)listener.LocalEndpoint).Port; }
    [Fact]
    public async Task TelaEGradeExigemHttpsTokenERespeitamBloqueio()
    {
        using var cert = CertificadoLocal.Gerar("127.0.0.1"); var pc = new PcTela(); var p1 = Porta(); var p2 = Porta();
        await using var app = HospedeJarvis.Criar(Array.Empty<string>(), p1, p2, cert, pc); await app.StartAsync();
        var aparelhos = app.Services.GetRequiredService<Aparelhos>(); string? token = null;
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{p1}") };
            Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync("/API/TELA")).StatusCode);
            using var https = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, c, _, _) => c?.Thumbprint == cert.Thumbprint }) { BaseAddress = new Uri($"https://127.0.0.1:{p2}") };
            Assert.Equal(HttpStatusCode.Unauthorized, (await https.GetAsync("/api/tela")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await https.PostAsJsonAsync("/api/janelas/layout", new PedidoOrganizacao(new("Metade", "principal", new long[] { 11, 12 })))).StatusCode);
            token = aparelhos.Parear("Teste de tela temporário"); https.DefaultRequestHeaders.Add("X-Jarvis-Token", token);
            Assert.Equal((HttpStatusCode)423, (await https.GetAsync("/api/tela")).StatusCode);
            pc.Ativa = true;
            using var telaSecundaria = await https.GetAsync("/api/tela?monitor=" + Uri.EscapeDataString(@"\\.\DISPLAY2")); telaSecundaria.EnsureSuccessStatusCode(); Assert.Equal(@"\\.\DISPLAY2", pc.MonitorRecebido);
            using var frame = await https.GetAsync("/api/tela"); Assert.Equal("image/jpeg", frame.Content.Headers.ContentType?.MediaType); Assert.True(frame.Headers.CacheControl?.NoStore); Assert.Equal(4, (await frame.Content.ReadAsByteArrayAsync()).Length);
            pc.Ativa = false; Assert.Equal((HttpStatusCode)423, (await https.GetAsync("/api/tela")).StatusCode);
            Assert.Single((await https.GetFromJsonAsync<List<MonitorInfo>>("/api/monitores"))!);
            using var layout = await https.PostAsJsonAsync("/api/janelas/layout", new PedidoOrganizacao(new("Metade", "principal", new long[] { 12, 11 }), true)); layout.EnsureSuccessStatusCode();
            Assert.Equal(new long[] { 12, 11 }, pc.Recebido!.Janelas);
            Assert.Equal(HttpStatusCode.BadRequest, (await https.PostAsJsonAsync("/api/janelas/layout", new PedidoOrganizacao(new("executar", "principal", new long[] { 1 })))).StatusCode);
            Assert.Contains("fechar_janela", ComandosFixos.Permitidos);
        }
        finally { if (token is not null) aparelhos.Remover(token); await app.StopAsync(); }
    }
}
