using Android.Content.PM;
using Android.Views;
namespace Jarvis.Mobile.Services;
internal static class TelaCheiaAndroid
{
    private static ScreenOrientation? _anterior;
    public static void Definir(bool ativa)
    {
        var activity = Platform.CurrentActivity; if (activity?.Window is not { } window) return;
        if (ativa) { _anterior ??= activity.RequestedOrientation; activity.RequestedOrientation = ScreenOrientation.SensorLandscape; window.AddFlags(WindowManagerFlags.Fullscreen | WindowManagerFlags.KeepScreenOn); }
        else { if (_anterior.HasValue) activity.RequestedOrientation = _anterior.Value; _anterior = null; window.ClearFlags(WindowManagerFlags.Fullscreen | WindowManagerFlags.KeepScreenOn); }
        if (OperatingSystem.IsAndroidVersionAtLeast(30) && window.InsetsController is { } controller)
        {
            if (ativa) { controller.Hide(WindowInsets.Type.SystemBars()); controller.SystemBarsBehavior = (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe; }
            else controller.Show(WindowInsets.Type.SystemBars());
        }
    }
}
