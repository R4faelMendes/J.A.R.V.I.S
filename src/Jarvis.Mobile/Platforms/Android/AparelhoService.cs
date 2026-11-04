using Android.App;
using Android.Content;
using Android.OS;
using Android.Media;
using Jarvis.Core;
using OperationCanceledException = System.OperationCanceledException;
namespace Jarvis.Mobile.Services;

[Service(Exported = false, ForegroundServiceType = Android.Content.PM.ForegroundService.TypeConnectedDevice)]
public sealed class AparelhoService : Service
{
    public static bool Ativo { get; private set; }
    private const string Channel = "jarvis_aparelho";
    private const int Id = 7643;
    private CancellationTokenSource? _life, _somLife;
    private Ringtone? _som;
    private readonly Dictionary<Guid, string> _confirmados = new();
    public override IBinder? OnBind(Intent? intent) => null;
    public static void Iniciar()
    {
        var c = Android.App.Application.Context; using var intent = new Intent(c, typeof(AparelhoService));
        if (OperatingSystem.IsAndroidVersionAtLeast(26)) c.StartForegroundService(intent); else c.StartService(intent);
    }
    public static void Parar() { var c = Android.App.Application.Context; c.StopService(new Intent(c, typeof(AparelhoService))); }
    public override void OnCreate()
    {
        base.OnCreate(); Ativo = true;
        if (OperatingSystem.IsAndroidVersionAtLeast(26)) ((NotificationManager)GetSystemService(NotificationService)!).CreateNotificationChannel(new NotificationChannel(Channel, "Seu celular no Jarvis", NotificationImportance.Low));
        StartForeground(Id, Notificacao());
        App.Conexao.IniciarMonitor(); _life = new(); var token = _life.Token; _ = Task.Run(() => LoopAsync(token));
    }
    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (intent?.Action == "silenciar") Silenciar();
        if (intent?.Action == "desconectar") { Preferences.Set("painel_aparelho", false); EspelhoCelular.Parar(); StopSelf(); }
        return StartCommandResult.NotSticky;
    }
    private Notification Notificacao()
    {
        using var builder = OperatingSystem.IsAndroidVersionAtLeast(26) ? new Notification.Builder(this, Channel) : new Notification.Builder(this);
        using var open = PendingIntent.GetActivity(this, Id, new Intent(this, typeof(MainActivity)).PutExtra("pagina", "aparelho").SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop), AndroidCompat.IntentFlags);
        using var mute = PendingIntent.GetService(this, Id + 1, new Intent(this, typeof(AparelhoService)).SetAction("silenciar"), AndroidCompat.IntentFlags);
        using var stop = PendingIntent.GetService(this, Id + 2, new Intent(this, typeof(AparelhoService)).SetAction("desconectar"), AndroidCompat.IntentFlags);
        builder.SetSmallIcon(Resource.Drawable.notification_jarvis); builder.SetContentTitle("Seu celular no Jarvis"); builder.SetContentText("Painel e localizar ativos · toque para gerenciar"); builder.SetOngoing(true); builder.SetOnlyAlertOnce(true); builder.SetContentIntent(open); builder.SetVisibility(NotificationVisibility.Private);
        AndroidCompat.AdicionarAcao(builder, Android.Resource.Drawable.IcMediaPause, "Parar som", mute); AndroidCompat.AdicionarAcao(builder, Android.Resource.Drawable.IcMenuCloseClearCancel, "Desconectar", stop); return builder.Build();
    }
    private async Task LoopAsync(CancellationToken ct)
    {
        try
        {
            await App.Conexao.CarregarSeNecessarioAsync();
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var dados = await MainThread.InvokeOnMainThreadAsync(DispositivoCompanheiro.Coletar);
                    var orders = await App.Conexao.PulsoAparelhoAsync(dados, ct);
                    DispositivoCompanheiro.Estado = "Painel conectado ao PC · localizar disponível.";
                    foreach (var order in orders)
                    {
                        if (!_confirmados.TryGetValue(order.Id, out var result))
                        {
                            result = "parado";
                            if (order.Acao == "localizar") result = await MainThread.InvokeOnMainThreadAsync(Tocar);
                            else if (order.Acao == "silenciar") { await MainThread.InvokeOnMainThreadAsync(Silenciar); result = "silenciado"; }
                            else if (order.Acao == "parar-espelho") await MainThread.InvokeOnMainThreadAsync(EspelhoCelular.Parar);
                            if (_confirmados.Count >= 64) _confirmados.Clear(); _confirmados[order.Id] = result;
                        }
                        await App.Conexao.ConfirmarAparelhoAsync(new(order.Id, result), ct);
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
                catch { DispositivoCompanheiro.Estado = "PC sem resposta · reconectando pelo Wi-Fi ou internet direta."; }
                await Task.Delay(4000, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch { DispositivoCompanheiro.Estado = "Abra o app para restabelecer a conexão."; }
    }
    private string Tocar()
    {
        Silenciar();
        try
        {
            var uri = RingtoneManager.GetDefaultUri(RingtoneType.Alarm) ?? RingtoneManager.GetDefaultUri(RingtoneType.Notification);
            _som = RingtoneManager.GetRingtone(this, uri); if (_som is null) return "erro";
            using var attributes = new AudioAttributes.Builder(); _som.AudioAttributes = attributes.SetUsage(AudioUsageKind.Alarm)!.SetContentType(AudioContentType.Sonification)!.Build();
            if (OperatingSystem.IsAndroidVersionAtLeast(28)) _som.Looping = true;
            _som.Play(); _somLife = new(); _ = EncerrarSomAsync(_somLife.Token); return "tocando";
        }
        catch { Silenciar(); return "erro"; }
    }
    private async Task EncerrarSomAsync(CancellationToken ct)
    {
        try
        {
            for (var i = 0; i < 20; i++) { await Task.Delay(1000, ct); if (!OperatingSystem.IsAndroidVersionAtLeast(28)) await MainThread.InvokeOnMainThreadAsync(() => { if (_som is not null && !_som.IsPlaying) _som.Play(); }); }
            await MainThread.InvokeOnMainThreadAsync(Silenciar);
        }
        catch (OperationCanceledException) { }
    }
    private void Silenciar() { _somLife?.Cancel(); _somLife?.Dispose(); _somLife = null; _som?.Stop(); _som?.Dispose(); _som = null; }
    public override void OnDestroy()
    {
        Ativo = false; _life?.Cancel(); _life?.Dispose(); _life = null; Silenciar();
        if (!App.EmPrimeiroPlano && !MidiaNotificacao.EmExecucao && !EspelhoCelular.Ativo) App.Conexao.PararMonitor();
        DispositivoCompanheiro.Estado = "Painel e localizar desativados.";
        if (OperatingSystem.IsAndroidVersionAtLeast(24)) StopForeground(StopForegroundFlags.Remove); else StopForeground(true);
        base.OnDestroy();
    }
}
