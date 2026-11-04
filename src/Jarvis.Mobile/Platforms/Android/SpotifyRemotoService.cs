using Android.App;
using Android.Content;
using Android.OS;
using Android.Media;
using Android.Media.Session;
using Jarvis.Core;
namespace Jarvis.Mobile.Services;

[Service(Exported = false, ForegroundServiceType = Android.Content.PM.ForegroundService.TypeMediaPlayback)]
public sealed class SpotifyRemotoService : Service
{
    public static bool Ativo { get; private set; }
    private const int NotificationId = 7641;
    private const string Channel = "jarvis_spotify_remoto";
    private CancellationTokenSource? _life;
    private MediaSession? _session;
    private EstadoMidia _state = new(false, "Conectando ao Spotify do PC…", "", "Spotify", false);
    private Android.Graphics.Bitmap? _cover;
    private readonly VolumeProvider _remoteVolume = new VolumeRemoto();
    public override IBinder? OnBind(Intent? intent) => null;
    public static void Iniciar()
    {
        var context = Android.App.Application.Context; var intent = new Intent(context, typeof(SpotifyRemotoService));
        if (OperatingSystem.IsAndroidVersionAtLeast(26)) context.StartForegroundService(intent); else context.StartService(intent);
    }
    public static void Parar() { var context = Android.App.Application.Context; context.StopService(new Intent(context, typeof(SpotifyRemotoService))); }
    public override void OnCreate()
    {
        base.OnCreate();
        Ativo = true; App.Conexao.IniciarMonitor();
        if (OperatingSystem.IsAndroidVersionAtLeast(26)) { var manager = (NotificationManager)GetSystemService(NotificationService)!; manager.CreateNotificationChannel(new NotificationChannel(Channel, "Spotify no computador", NotificationImportance.Low)); }
        _session = new MediaSession(this, "JarvisSpotifyRemoto"); _session.SetFlags(MediaSessionFlags.HandlesMediaButtons | MediaSessionFlags.HandlesTransportControls); _session.SetCallback(new Controles(this)); _session.SetPlaybackToRemote(_remoteVolume); _session.Active = true;
        StartForeground(NotificationId, CriarNotificacao());
        _life = new CancellationTokenSource(); _ = MonitorarAsync(_life.Token);
    }
    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (intent?.Action == "parar") { StopSelf(); return StartCommandResult.NotSticky; }
        if (intent?.Action is "midia_anterior" or "midia_alternar" or "midia_proxima") _ = ComandoAsync(intent.Action);
        return StartCommandResult.NotSticky;
    }
    private async Task MonitorarAsync(CancellationToken ct)
    {
        try
        {
            await App.Conexao.CarregarSeNecessarioAsync();
            while (!ct.IsCancellationRequested)
            {
                try { _state = await App.Conexao.MidiaAsync(ct); }
                catch (System.OperationCanceledException) when (ct.IsCancellationRequested) { break; }
                catch { _state = new(false, "PC offline · reconectando", "O controle requer conexão com o computador", "Spotify", false); }
                await MainThread.InvokeOnMainThreadAsync(() => { if (!ct.IsCancellationRequested) Atualizar(); });
                await Task.Delay(3000, ct);
            }
        }
        catch (System.OperationCanceledException) { }
    }
    private async Task ComandoAsync(string name)
    {
        try { await App.Conexao.ComandoAsync(name); _state = await App.Conexao.MidiaAsync(); await MainThread.InvokeOnMainThreadAsync(Atualizar); }
        catch { _state = new(false, "Comando não enviado · PC offline", "Verifique a rede privada ou o Wi-Fi", "Spotify", false); await MainThread.InvokeOnMainThreadAsync(Atualizar); }
    }
    private void Atualizar()
    {
        if (_session is null) return;
        var old = _cover; _cover = _state.Capa is { Length: > 0 } bytes ? Android.Graphics.BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length) : null;
        using var metadata = new MediaMetadata.Builder(); metadata.PutString(MediaMetadata.MetadataKeyTitle, _state.Titulo); metadata.PutString(MediaMetadata.MetadataKeyArtist, _state.Artista); if (_cover is not null) metadata.PutBitmap(MediaMetadata.MetadataKeyAlbumArt, _cover); _session.SetMetadata(metadata.Build());
        using var playback = new PlaybackState.Builder(); playback.SetActions(PlaybackState.ActionPlay | PlaybackState.ActionPause | PlaybackState.ActionPlayPause | PlaybackState.ActionSkipToNext | PlaybackState.ActionSkipToPrevious); playback.SetState(_state.Tocando ? PlaybackStateCode.Playing : PlaybackStateCode.Paused, (long)(_state.PosicaoSegundos * 1000), _state.Tocando ? 1 : 0); _session.SetPlaybackState(playback.Build());
        ((NotificationManager)GetSystemService(NotificationService)!).Notify(NotificationId, CriarNotificacao()); old?.Dispose();
    }
    private PendingIntent Acao(string action, int code) => PendingIntent.GetService(this, code, new Intent(this, typeof(SpotifyRemotoService)).SetAction(action), AndroidCompat.IntentFlags)!;
    private Notification CriarNotificacao()
    {
        using var builder = OperatingSystem.IsAndroidVersionAtLeast(26) ? new Notification.Builder(this, Channel) : new Notification.Builder(this);
        using var previous = Acao("midia_anterior", 1); using var toggle = Acao("midia_alternar", 2); using var next = Acao("midia_proxima", 3); using var stop = Acao("parar", 4);
        using var open = PendingIntent.GetActivity(this, 7641, new Intent(this, typeof(MainActivity)).PutExtra("pagina", "midia").SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop), AndroidCompat.IntentFlags);
        builder.SetSmallIcon(Resource.Drawable.notification_jarvis).SetContentTitle(_state.Titulo).SetContentText(_state.Artista + " · Spotify no PC").SetOnlyAlertOnce(true).SetContentIntent(open).SetVisibility(NotificationVisibility.Private).SetDeleteIntent(stop);
        void Add(int icon, string title, PendingIntent pending)
        {
            using var action = OperatingSystem.IsAndroidVersionAtLeast(23) ? new Notification.Action.Builder(Android.Graphics.Drawables.Icon.CreateWithResource(this, icon), title, pending) : new Notification.Action.Builder(icon, title, pending);
            builder.AddAction(action.Build());
        }
        Add(Android.Resource.Drawable.IcMediaPrevious, "Anterior", previous); Add(_state.Tocando ? Android.Resource.Drawable.IcMediaPause : Android.Resource.Drawable.IcMediaPlay, "Tocar / pausar", toggle); Add(Android.Resource.Drawable.IcMediaNext, "Próxima", next); Add(Android.Resource.Drawable.IcMenuCloseClearCancel, "Encerrar controle", stop);
        if (_cover is not null) builder.SetLargeIcon(_cover);
        using var style = new Notification.MediaStyle(); style.SetShowActionsInCompactView(0, 1, 2); if (_session is not null) style.SetMediaSession(_session.SessionToken); builder.SetStyle(style);
        return builder.Build();
    }
    public override void OnDestroy() { Ativo = false; if (!App.EmPrimeiroPlano && !DispositivoCompanheiro.Ativo && !EspelhoCelular.Ativo) App.Conexao.PararMonitor(); _life?.Cancel(); _life?.Dispose(); _life = null; _session?.Release(); _session?.Dispose(); _session = null; _remoteVolume.Dispose(); _cover?.Dispose(); _cover = null; if (OperatingSystem.IsAndroidVersionAtLeast(24)) StopForeground(StopForegroundFlags.Remove); else StopForeground(true); base.OnDestroy(); }
    private sealed class VolumeRemoto() : VolumeProvider(VolumeControl.Fixed, 100, 100);
    private sealed class Controles(SpotifyRemotoService service) : MediaSession.Callback
    {
        public override void OnPlay() => _ = service.ComandoAsync("midia_continuar");
        public override void OnPause() => _ = service.ComandoAsync("midia_pausar");
        public override void OnSkipToNext() => _ = service.ComandoAsync("midia_proxima");
        public override void OnSkipToPrevious() => _ = service.ComandoAsync("midia_anterior");
    }
}



