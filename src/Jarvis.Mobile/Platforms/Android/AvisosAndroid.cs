using Android.App;
using Android.Content;
using Android.OS;
using System.Text.Json;
using Jarvis.Core;
namespace Jarvis.Mobile.Services;

internal static class AvisosAndroid
{
    private const string Canal = "jarvis_agenda";
    private const string Store = "jarvis_lembretes";
    private sealed record Alerta(string Id, string Titulo, long Quando, long Inicio);
    private static int Codigo(string id) => BitConverter.ToInt32(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(id)), 0) & int.MaxValue;
    private static Alerta[] Ler(Context context) { try { return JsonSerializer.Deserialize<Alerta[]>(context.GetSharedPreferences(Store, FileCreationMode.Private)!.GetString("alertas", "[]")!) ?? []; } catch { return []; } }
    private static PendingIntent Pedido(Context c, Alerta a)
    {
        var intent = new Intent(c, typeof(AlertaAgendaReceiver)); intent.SetData(Android.Net.Uri.Parse("jarvis://alerta/" + a.Id)); intent.PutExtra("id", a.Id);
        return PendingIntent.GetBroadcast(c, Codigo(a.Id), intent, AndroidCompat.IntentFlags)!;
    }
    public static void Reagendar(Context context, IEnumerable<Compromisso> items)
    {
        var manager = (AlarmManager)context.GetSystemService(Context.AlarmService)!; var prefs = context.GetSharedPreferences(Store, FileCreationMode.Private)!;
        var alerts = items.Where(c => c.AlertaEm.HasValue && !c.Excluido).Select(c => new Alerta(c.Id, c.Titulo, c.AlertaEm!.Value.ToUnixTimeMilliseconds(), c.Inicio.ToUnixTimeMilliseconds())).ToArray();
        foreach (var old in Ler(context)) { using var pending = Pedido(context, old); manager.Cancel(pending); }
        prefs.Edit()!.PutString("alertas", JsonSerializer.Serialize(alerts))!.Commit();
        Registrar(context, alerts);
    }
    public static void Restaurar(Context context) => Registrar(context, Ler(context));
    private static void Registrar(Context context, Alerta[] alerts)
    {
        var manager = (AlarmManager)context.GetSystemService(Context.AlarmService)!; var prefs = context.GetSharedPreferences(Store, FileCreationMode.Private)!; var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        foreach (var alert in alerts)
        {
            if (alert.Inicio < now || prefs.GetBoolean("enviado-" + alert.Id + "-" + alert.Quando, false)) continue;
            using var pending = Pedido(context, alert); var at = Math.Max(alert.Quando, now + 2000);
            try
            {
                if (!OperatingSystem.IsAndroidVersionAtLeast(23)) manager.SetExact(AlarmType.RtcWakeup, at, pending);
                else if (!OperatingSystem.IsAndroidVersionAtLeast(31) || manager.CanScheduleExactAlarms()) manager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, at, pending);
                else manager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, at, pending);
            }
            catch (Java.Lang.SecurityException) { if (OperatingSystem.IsAndroidVersionAtLeast(23)) manager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, at, pending); else manager.Set(AlarmType.RtcWakeup, at, pending); }
        }
    }
    public static void PedirAlarmes()
    {
        var context = Android.App.Application.Context; var manager = (AlarmManager)context.GetSystemService(Context.AlarmService)!;
        if (OperatingSystem.IsAndroidVersionAtLeast(31) && !manager.CanScheduleExactAlarms())
        {
            var intent = new Intent(Android.Provider.Settings.ActionRequestScheduleExactAlarm, Android.Net.Uri.Parse("package:" + context.PackageName)); intent.AddFlags(ActivityFlags.NewTask); context.StartActivity(intent);
        }
    }
    public static void Mostrar(Context context, string? id)
    {
        var alert = Ler(context).FirstOrDefault(a => a.Id == id); if (alert is null) return;
        var prefs = context.GetSharedPreferences(Store, FileCreationMode.Private)!; var key = "enviado-" + alert.Id + "-" + alert.Quando; if (prefs.GetBoolean(key, false)) return;
        var manager = (NotificationManager)context.GetSystemService(Context.NotificationService)!;
        if (OperatingSystem.IsAndroidVersionAtLeast(26)) { using var channel = new NotificationChannel(Canal, "Compromissos da agenda", NotificationImportance.High); channel.EnableVibration(true); manager.CreateNotificationChannel(channel); }
        var launch = new Intent(context, typeof(MainActivity)); launch.PutExtra("pagina", "agenda"); launch.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        using var pending = PendingIntent.GetActivity(context, Codigo(alert.Id), launch, AndroidCompat.IntentFlags);
        using var builder = OperatingSystem.IsAndroidVersionAtLeast(26) ? new Notification.Builder(context, Canal) : new Notification.Builder(context);
        builder.SetSmallIcon(Resource.Drawable.notification_jarvis).SetContentTitle(alert.Titulo).SetContentText("Compromisso às " + DateTimeOffset.FromUnixTimeMilliseconds(alert.Inicio).ToLocalTime().ToString("HH:mm")).SetContentIntent(pending).SetAutoCancel(true).SetVisibility(NotificationVisibility.Private);
        if (!OperatingSystem.IsAndroidVersionAtLeast(33) || context.CheckSelfPermission(Android.Manifest.Permission.PostNotifications) == Android.Content.PM.Permission.Granted) { manager.Notify(Codigo(alert.Id), builder.Build()); prefs.Edit()!.PutBoolean(key, true)!.Apply(); }
    }
}

[BroadcastReceiver(Enabled = true, Exported = false)]
public sealed class AlertaAgendaReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent) { if (context is not null) AvisosAndroid.Mostrar(context, intent?.GetStringExtra("id")); }
}
[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter(new[] { Intent.ActionBootCompleted, Intent.ActionMyPackageReplaced, Intent.ActionTimeChanged, Intent.ActionTimezoneChanged, "android.app.action.SCHEDULE_EXACT_ALARM_PERMISSION_STATE_CHANGED" })]
public sealed class RestaurarAgendaReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent) { if (context is not null) AvisosAndroid.Restaurar(context); }
}

