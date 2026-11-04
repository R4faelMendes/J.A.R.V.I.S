using Android.App;
namespace Jarvis.Mobile.Services;
internal static class AndroidCompat
{
    public static PendingIntentFlags IntentFlags => PendingIntentFlags.UpdateCurrent | (OperatingSystem.IsAndroidVersionAtLeast(23) ? PendingIntentFlags.Immutable : 0);
    public static void AdicionarAcao(Notification.Builder builder, int icon, string title, PendingIntent? pending)
    {
        using var action = OperatingSystem.IsAndroidVersionAtLeast(23) ? new Notification.Action.Builder(Android.Graphics.Drawables.Icon.CreateWithResource(Android.App.Application.Context, icon), title, pending) : new Notification.Action.Builder(icon, title, pending);
        builder.AddAction(action.Build());
    }
}
