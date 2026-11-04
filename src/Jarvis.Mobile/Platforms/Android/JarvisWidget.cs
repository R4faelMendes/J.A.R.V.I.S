using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using Jarvis.Core;
using System.Text.Json;
namespace Jarvis.Mobile;

[BroadcastReceiver(Name = "com.companyname.jarvis.mobile.JarvisWidget", Label = "Jarvis · Agenda", Exported = true)]
[IntentFilter(new[] { AppWidgetManager.ActionAppwidgetUpdate })]
[MetaData(AppWidgetManager.MetaDataAppwidgetProvider, Resource = "@xml/jarvis_widget_info")]
public sealed class JarvisWidget : AppWidgetProvider
{
    private sealed record AgendaCache(Compromisso[] Itens, DateTimeOffset Atualizado);
    private static readonly object Gate = new();
    private static string Arquivo => Path.Combine(Android.App.Application.Context.FilesDir!.AbsolutePath, "widget-agenda.json");
    public static void PublicarAgenda(IEnumerable<Compromisso> itens) {
        lock (Gate) { File.WriteAllText(Arquivo + ".tmp", JsonSerializer.Serialize(new AgendaCache(itens.Where(c => !c.Excluido).ToArray(), DateTimeOffset.Now))); File.Move(Arquivo + ".tmp", Arquivo, true); }
        AtualizarTodos();
    }
    public override void OnUpdate(Context? context, AppWidgetManager? manager, int[]? ids)
    {
        if (context is null || manager is null || ids is null) return;
        AgendaCache? cache; lock (Gate) { try { cache = JsonSerializer.Deserialize<AgendaCache>(File.ReadAllText(Arquivo)); } catch { cache = null; } }
        var upcoming = AgendaWidget.Proximos(cache?.Itens ?? [], DateTimeOffset.Now);
        foreach (var id in ids) {
            using var views = new RemoteViews(context.PackageName, Resource.Layout.jarvis_widget);
            views.SetTextColor(Resource.Id.jarvis_widget_title, Android.Graphics.Color.ParseColor(Jarvis.Core.TemasJarvis.Atual.Acento));
            views.SetTextViewText(Resource.Id.jarvis_widget_updated, cache is null ? "Abra a agenda para sincronizar" : "Agenda local · atualizada " + cache.Atualizado.LocalDateTime.ToString("dd/MM HH:mm"));
            var rows = new[] { Resource.Id.jarvis_widget_event1, Resource.Id.jarvis_widget_event2, Resource.Id.jarvis_widget_event3, Resource.Id.jarvis_widget_event4 };
            for (var i = 0; i < rows.Length; i++) {
                var item = i < upcoming.Count ? upcoming[i] : null;
                views.SetTextViewText(rows[i], item is not null ? $"{item.Inicio.LocalDateTime:dd/MM · HH:mm}   {item.Titulo}" : i == 0 ? "Nenhum compromisso próximo." : "");
                views.SetViewVisibility(rows[i], item is not null || i == 0 ? Android.Views.ViewStates.Visible : Android.Views.ViewStates.Gone);
            }
            void Link(int view, string route, int code) {
                using var intent = new Intent(context, typeof(MainActivity)); intent.PutExtra("pagina", route);
                intent.SetAction("jarvis.widget." + route); intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
                var flags = PendingIntentFlags.UpdateCurrent;
                if (OperatingSystem.IsAndroidVersionAtLeast(23)) flags |= PendingIntentFlags.Immutable;
                using var pending = PendingIntent.GetActivity(context, code, intent, flags);
                views.SetOnClickPendingIntent(view, pending);
            }
            Link(Resource.Id.jarvis_widget_agenda, "agenda", 7103);
            foreach (var row in rows) Link(row, "agenda", 7103);
            manager.UpdateAppWidget(id, views);
        }
    }
    public static bool SolicitarFixacao()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return false;
        var context = Android.App.Application.Context;
        var manager = AppWidgetManager.GetInstance(context);
        if (manager?.IsRequestPinAppWidgetSupported != true) return false;
        using var component = new ComponentName(context, Java.Lang.Class.FromType(typeof(JarvisWidget)));
        return manager.RequestPinAppWidget(component, null, null);
    }
    public static void AtualizarTodos()
    {
        var context = Android.App.Application.Context; var manager = AppWidgetManager.GetInstance(context);
        using var component = new ComponentName(context, Java.Lang.Class.FromType(typeof(JarvisWidget)));
        var ids = manager?.GetAppWidgetIds(component); if (ids is null || ids.Length == 0) return;
        using var provider = new JarvisWidget(); provider.OnUpdate(context, manager, ids);
    }
}
