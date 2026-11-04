using Android.App;
using Android.Content;
using Android.OS;
using Android.Media;
using Android.Media.Projection;
using Android.Hardware.Display;
using Android.Graphics;
using OperationCanceledException = System.OperationCanceledException;
namespace Jarvis.Mobile.Services;

[Service(Exported = false, ForegroundServiceType = Android.Content.PM.ForegroundService.TypeMediaProjection)]
public sealed class EspelhoCelularService : Service
{
    public static bool Ativo { get; private set; }
    private static EspelhoCelularService? _atual;
    public static bool SessaoValida(Guid sessao) => _atual?._sessao == sessao && Ativo && !_atual._encerrando;
    private const int Id = 7644;
    private const string Channel = "jarvis_espelho_celular";
    private readonly object _capture = new();
    private CancellationTokenSource? _life;
    private MediaProjection? _projection;
    private Callback? _callback;
    private Handler? _handler;
    private VirtualDisplay? _display;
    private ImageReader? _reader;
    private int _width, _height, _density;
    private Guid? _sessao;
    private bool _encerrando;
    private BroadcastReceiver? _screenOff;
    public override IBinder? OnBind(Intent? intent) => null;
    public static void Iniciar(Context c, int resultado, Intent dados)
    {
        using var intent = new Intent(c, typeof(EspelhoCelularService)).PutExtra("resultado", resultado).PutExtra("dados", dados);
        if (OperatingSystem.IsAndroidVersionAtLeast(26)) c.StartForegroundService(intent); else c.StartService(intent);
    }
    public static void Parar() { var c = Android.App.Application.Context; c.StopService(new Intent(c, typeof(EspelhoCelularService))); }
    public override void OnCreate()
    {
        base.OnCreate();
        if (OperatingSystem.IsAndroidVersionAtLeast(26)) ((NotificationManager)GetSystemService(NotificationService)!).CreateNotificationChannel(new NotificationChannel(Channel, "Tela do celular no PC", NotificationImportance.Low));
        _screenOff = new TelaDesligada(this);
        if (OperatingSystem.IsAndroidVersionAtLeast(33)) RegisterReceiver(_screenOff, new IntentFilter(Intent.ActionScreenOff), ReceiverFlags.NotExported); else RegisterReceiver(_screenOff, new IntentFilter(Intent.ActionScreenOff));
    }
    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (intent?.Action == "parar") { StopSelf(); return StartCommandResult.NotSticky; }
        if (_life is not null) return StartCommandResult.NotSticky;
        try
        {
            using var builder = OperatingSystem.IsAndroidVersionAtLeast(26) ? new Notification.Builder(this, Channel) : new Notification.Builder(this);
            using var stop = PendingIntent.GetService(this, Id, new Intent(this, typeof(EspelhoCelularService)).SetAction("parar"), AndroidCompat.IntentFlags);
            using var open = PendingIntent.GetActivity(this, Id, new Intent(this, typeof(MainActivity)).PutExtra("pagina", "aparelho").SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop), AndroidCompat.IntentFlags);
            builder.SetSmallIcon(Resource.Drawable.notification_jarvis); builder.SetContentTitle("Sua tela está sendo enviada ao Jarvis"); builder.SetContentText("Sem áudio · toque em Parar para encerrar"); builder.SetOngoing(true); builder.SetContentIntent(open); builder.SetVisibility(NotificationVisibility.Private); AndroidCompat.AdicionarAcao(builder, Android.Resource.Drawable.IcMenuCloseClearCancel, "Parar", stop);
            StartForeground(Id, builder.Build());
            var data = intent is null ? null : OperatingSystem.IsAndroidVersionAtLeast(33) ? intent.GetParcelableExtra("dados", Java.Lang.Class.FromType(typeof(Intent))) as Intent : intent.GetParcelableExtra("dados") as Intent;
            if (data is null || intent!.GetIntExtra("resultado", 0) != (int)Result.Ok) throw new InvalidOperationException("Autorize a captura no celular.");
            var manager = (MediaProjectionManager)GetSystemService(MediaProjectionService)!;
            _projection = manager.GetMediaProjection((int)Result.Ok, data) ?? throw new InvalidOperationException("Autorização de tela inválida.");
            _handler = new Handler(Looper.MainLooper!); _callback = new Callback(this); _projection.RegisterCallback(_callback, _handler);
            var metrics = Resources!.DisplayMetrics!; _density = (int)metrics.DensityDpi;
            var dimensoes = ControleCelularAndroid.Tela(); Redimensionar(dimensoes.Largura, dimensoes.Altura);
            _display = _projection.CreateVirtualDisplay("JarvisCelular", _width, _height, _density, (Android.Views.DisplayFlags)VirtualDisplayFlags.AutoMirror, _reader!.Surface, null, _handler);
            _life = new(); var token = _life.Token; _atual = this; Ativo = true; EspelhoCelular.Estado = "Enviando a tela ao PC · use a notificação para parar."; _ = Task.Run(() => LoopAsync(token));
        }
        catch (Exception ex) { EspelhoCelular.Estado = "Não foi possível iniciar: " + ex.Message; StopSelf(); }
        return StartCommandResult.NotSticky;
    }
    private void Redimensionar(int width, int height)
    {
        if (width <= 0 || height <= 0 || _encerrando) return;
        var scale = Math.Min(1, 1280d / Math.Max(width, height)); var w = Math.Max(1, (int)(width * scale)); var h = Math.Max(1, (int)(height * scale));
        lock (_capture)
        {
            if (w == _width && h == _height && _reader is not null) return;
            var old = _reader; _width = w; _height = h;
            _reader = ImageReader.NewInstance(w, h, (ImageFormatType)1, 3);
            if (_display is not null) { _display.Resize(w, h, _density); _display.Surface = _reader!.Surface; }
            old?.Close(); old?.Dispose();
        }
    }
    private Bitmap? Capturar()
    {
        if (((KeyguardManager)GetSystemService(KeyguardService)!).IsKeyguardLocked) throw new InvalidOperationException("Tela bloqueada.");
        lock (_capture)
        {
            using var image = _reader?.AcquireLatestImage(); if (image is null) return null;
            try
            {
            using var plane = image.GetPlanes()![0]; using var buffer = plane.Buffer!; var w = image.Width; var h = image.Height;
            if (plane.PixelStride != 4 || plane.RowStride < w * 4) throw new InvalidOperationException("Formato de tela não suportado neste aparelho.");
            var length = w * h * 4; var pixels = System.Buffers.ArrayPool<byte>.Shared.Rent(length);
            try
            {
                for (var row = 0; row < h; row++) { buffer.Position(row * plane.RowStride); buffer.Get(pixels, row * w * 4, w * 4); }
                var bitmap = Bitmap.CreateBitmap(w, h, Bitmap.Config.Argb8888!)!;
                try { using var packed = Java.Nio.ByteBuffer.Wrap(pixels, 0, length); bitmap.CopyPixelsFromBuffer(packed); bitmap.HasAlpha = false; return bitmap; } catch { bitmap.Dispose(); throw; }
            }
            finally { System.Buffers.ArrayPool<byte>.Shared.Return(pixels, clearArray: true); }
            }
            finally { image.Close(); } // Dispose libera o wrapper JNI; Close devolve o buffer ao produtor.
        }
    }
    private async Task LoopAsync(CancellationToken ct)
    {
        try
        {
            await App.Conexao.CarregarSeNecessarioAsync(); var pareamento = App.Conexao.IdentificadorPareamento;
            await App.Conexao.PulsoAparelhoAsync(await MainThread.InvokeOnMainThreadAsync(DispositivoCompanheiro.Coletar), ct);
            _sessao = await App.Conexao.IniciarEspelhoCelularAsync(ct);
            var controle = ControleCelularAndroid.Suportado ? ControlarAsync(_sessao.Value, pareamento, ct) : Task.CompletedTask;
            byte[]? ultimoJpeg = null; var ultimaRotacao = ControleCelularAndroid.Tela().Rotacao; var ultimoEnvio = DateTimeOffset.MinValue; var primeiroQuadro = System.Diagnostics.Stopwatch.StartNew(); var pretos = 0;
            while (!ct.IsCancellationRequested)
            {
                var clock = System.Diagnostics.Stopwatch.StartNew();
                var tela = ControleCelularAndroid.Tela();
                if (tela.Rotacao != ultimaRotacao)
                {
                    // Antes do Android 14 não há callback de tamanho do conteúdo capturado.
                    if (!OperatingSystem.IsAndroidVersionAtLeast(34)) Redimensionar(tela.Largura, tela.Altura);
                    ultimoJpeg = null; ultimaRotacao = tela.Rotacao; primeiroQuadro.Restart();
                }
                using var captura = EspelhoCelular.Compatibilidade && ControleCelularAndroid.Ativo ? null : Capturar();
                if (captura is not null) pretos = ImagemPreta(captura) ? pretos + 1 : 0;
                var compativel = EspelhoCelular.Compatibilidade || pretos >= 4 || (ultimoJpeg is null && primeiroQuadro.Elapsed.TotalSeconds > 3);
                using var alternativa = compativel && ControleCelularAndroid.Ativo ? await ControleCelularAndroid.CapturarCompativelAsync(_sessao.Value, ct) : null;
                var bitmap = alternativa ?? captura;
                if (bitmap is not null && ControleCelularAndroid.Tela().Rotacao == ultimaRotacao)
                {
                    using var output = new MemoryStream(); if (!bitmap.Compress(Bitmap.CompressFormat.Jpeg!, 65, output)) throw new InvalidOperationException("O Android não conseguiu codificar a imagem.");
                    ultimoJpeg = output.ToArray();
                    EspelhoCelular.Estado = alternativa is not null ? "Enviando em modo compatível · mouse e teclado disponíveis." : pretos >= 4 ? (ControleCelularAndroid.Suportado ? "O Android enviou imagem preta. Teste o modo compatível na tela inicial." : "O Android enviou imagem preta. Pare o compartilhamento e teste novamente usando Tela inteira na tela inicial.") : "Enviando a tela ao PC · use a notificação para parar.";
                }
                // Alguns aparelhos só produzem uma imagem quando há mudança na tela.
                if (ultimoJpeg is not null && (bitmap is not null || DateTimeOffset.UtcNow - ultimoEnvio >= TimeSpan.FromSeconds(1)))
                { await App.Conexao.EnviarQuadroCelularAsync(_sessao.Value, ultimoJpeg, pareamento, ct, ultimaRotacao); ultimoEnvio = DateTimeOffset.UtcNow; }
                if (ultimoJpeg is null && primeiroQuadro.Elapsed > TimeSpan.FromSeconds(10)) throw new InvalidOperationException("O Android não produziu uma imagem da tela.");
                var delay = TimeSpan.FromMilliseconds(compativel ? 550 : 220) - clock.Elapsed; if (delay > TimeSpan.Zero) await Task.Delay(delay, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { EspelhoCelular.Estado = "Compartilhamento encerrado: " + ex.Message; }
        finally
        {
            if (_sessao is { } session) { try { await App.Conexao.PararEspelhoCelularAsync(session); } catch { } }
            if (!_encerrando) await MainThread.InvokeOnMainThreadAsync(StopSelf);
        }
    }
    public override void OnDestroy()
    {
        _encerrando = true; Ativo = false; if (ReferenceEquals(_atual, this)) _atual = null; _life?.Cancel(); _life?.Dispose(); _life = null;
        if (_screenOff is not null) { try { UnregisterReceiver(_screenOff); } catch { } _screenOff.Dispose(); _screenOff = null; }
        lock (_capture) { try { _display?.Release(); } catch (Java.Lang.Exception) { } _display?.Dispose(); _display = null; try { _reader?.Close(); } catch (Java.Lang.Exception) { } _reader?.Dispose(); _reader = null; }
        if (_projection is not null) { try { if (_callback is not null) _projection.UnregisterCallback(_callback); _projection.Stop(); } catch (Java.Lang.Exception) { } _projection.Dispose(); _projection = null; }
        _callback?.Dispose(); _callback = null; _handler?.Dispose(); _handler = null;
        if (EspelhoCelular.Estado.StartsWith("Enviando")) EspelhoCelular.Estado = "Compartilhamento parado.";
        if (!App.EmPrimeiroPlano && !DispositivoCompanheiro.Ativo && !MidiaNotificacao.EmExecucao) App.Conexao.PararMonitor();
        if (OperatingSystem.IsAndroidVersionAtLeast(24)) StopForeground(StopForegroundFlags.Remove); else StopForeground(true);
        base.OnDestroy();
    }
    private static bool ImagemPreta(Bitmap bitmap)
    {
        for (var y = 1; y < 9; y++) for (var x = 1; x < 9; x++)
        { var c = new Android.Graphics.Color(bitmap.GetPixel(x * bitmap.Width / 10, y * bitmap.Height / 10)); if (c.R > 12 || c.G > 12 || c.B > 12) return false; }
        return true;
    }
    private sealed class Callback(EspelhoCelularService service) : MediaProjection.Callback
    {
        public override void OnStop() { if (!service._encerrando) { EspelhoCelular.Estado = "Captura encerrada pelo Android. Autorize novamente para retomar."; service.StopSelf(); } }
        public override void OnCapturedContentResize(int width, int height) { try { service.Redimensionar(width, height); } catch { service.StopSelf(); } }
        public override void OnCapturedContentVisibilityChanged(bool isVisible) { if (!isVisible && !service._encerrando) EspelhoCelular.Estado = "Aguardando o conteúdo compartilhado ficar visível. Abra a tela inicial do Android."; }
    }
    private sealed class TelaDesligada(EspelhoCelularService service) : BroadcastReceiver
    { public override void OnReceive(Context? context, Intent? intent) { EspelhoCelular.Estado = "Compartilhamento encerrado ao desligar a tela."; service.StopSelf(); } }
    private static async Task ControlarAsync(Guid sessao, string pareamento, CancellationToken ct)
    {
        var processados = new Dictionary<Guid, string>();
        try
        {
            while (!ct.IsCancellationRequested && SessaoValida(sessao))
            {
                try
                {
                    var entradas = await App.Conexao.PuxarEntradasCelularAsync(sessao, pareamento, ct);
                    foreach (var order in entradas)
                    {
                        if (!processados.TryGetValue(order.Id, out var result))
                        {
                            result = "expirado";
                            if (order.Expira > DateTimeOffset.UtcNow && SessaoValida(sessao))
                                result = await MainThread.InvokeOnMainThreadAsync(async () => await ControleCelularAndroid.ExecutarAsync(order.Entrada, ct)) ? "ok" : "recusado";
                            if (processados.Count > 256) processados.Clear(); processados[order.Id] = result;
                        }
                        await App.Conexao.ConfirmarEntradaCelularAsync(new(order.Id, result), pareamento, ct);
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
                catch { await Task.Delay(500, ct); }
            }
        }
        catch (OperationCanceledException) { }
    }
}
