using Android.AccessibilityServices;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Views.Accessibility;
using Jarvis.Core;

namespace Jarvis.Mobile.Services;

[Service(Label = "Jarvis · mouse e teclado do PC", Permission = "android.permission.BIND_ACCESSIBILITY_SERVICE", Exported = true)]
[IntentFilter(["android.accessibilityservice.AccessibilityService"])]
[MetaData("android.accessibilityservice", Resource = "@xml/jarvis_acessibilidade")]
public sealed class ControleCelularAndroid : AccessibilityService
{
    public static bool Suportado => true;
    private static ControleCelularAndroid? _atual;
    public static bool Ativo => _atual is not null && OperatingSystem.IsAndroidVersionAtLeast(24);
    protected override void OnServiceConnected() { base.OnServiceConnected(); _atual = this; }
    public override void OnAccessibilityEvent(AccessibilityEvent? e) { }
    public override void OnInterrupt() { }
    public override void OnDestroy() { if (ReferenceEquals(_atual, this)) _atual = null; base.OnDestroy(); }
    public static async Task<Bitmap?> CapturarCompativelAsync(Guid sessao, CancellationToken ct)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(30) || _atual is not { } service || !EspelhoCelularService.SessaoValida(sessao) || ((KeyguardManager)service.GetSystemService(KeyguardService)!).IsKeyguardLocked) return null;
        var pending = new TaskCompletionSource<Bitmap?>(TaskCreationOptions.RunContinuationsAsynchronously);
        await MainThread.InvokeOnMainThreadAsync(() => { if (OperatingSystem.IsAndroidVersionAtLeast(30)) service.TakeScreenshot(Display.DefaultDisplay, service.MainExecutor!, new Captura(pending, sessao)); });
        try { return await pending.Task.WaitAsync(TimeSpan.FromSeconds(2), ct); }
        catch { pending.TrySetResult(null); ct.ThrowIfCancellationRequested(); return null; }
    }
    [System.Runtime.Versioning.SupportedOSPlatform("android30.0")]
    private sealed class Captura(TaskCompletionSource<Bitmap?> pending, Guid sessao) : Java.Lang.Object, ITakeScreenshotCallback
    {
        public void OnFailure(int errorCode) => pending.TrySetResult(null);
        public void OnSuccess(ScreenshotResult result)
        {
            using (result)
            using (var buffer = result.HardwareBuffer)
            {
                try
                {
                    if (!EspelhoCelularService.SessaoValida(sessao) || pending.Task.IsCompleted) { pending.TrySetResult(null); return; }
                    using var hardware = Bitmap.WrapHardwareBuffer(buffer!, result.ColorSpace!);
                    using var cpu = hardware?.Copy(Bitmap.Config.Argb8888!, false);
                    if (cpu is null) { pending.TrySetResult(null); return; }
                    var scale = Math.Min(1, 1280d / Math.Max(cpu.Width, cpu.Height));
                    var output = Bitmap.CreateScaledBitmap(cpu, Math.Max(1, (int)(cpu.Width * scale)), Math.Max(1, (int)(cpu.Height * scale)), true)!;
                    // CreateScaledBitmap pode devolver a mesma instância; o resultado precisa ser independente.
                    var copy = ReferenceEquals(output, cpu) ? cpu.Copy(Bitmap.Config.Argb8888!, false) : output;
                    if (!pending.TrySetResult(copy)) copy?.Dispose();
                }
                catch { pending.TrySetResult(null); }
                finally { buffer?.Close(); }
            }
        }
    }
    public static void Configurar()
    {
        var c = Android.App.Application.Context;
        try { using var intent = new Intent("android.settings.ACCESSIBILITY_DETAILS_SETTINGS").PutExtra("android.intent.extra.COMPONENT_NAME", new ComponentName(c, Java.Lang.Class.FromType(typeof(ControleCelularAndroid)))).AddFlags(ActivityFlags.NewTask); c.StartActivity(intent); }
        catch { c.StartActivity(new Intent(Android.Provider.Settings.ActionAccessibilitySettings).AddFlags(ActivityFlags.NewTask)); }
    }
    public static (int Largura, int Altura, int Rotacao) Tela()
    {
        var c = Android.App.Application.Context; var window = (IWindowManager)c.GetSystemService(WindowService)!;
        var rotation = (int)window.DefaultDisplay!.Rotation;
        if (OperatingSystem.IsAndroidVersionAtLeast(30)) { using var metrics = window.MaximumWindowMetrics; using var bounds = metrics!.Bounds; return (bounds.Width(), bounds.Height(), rotation); }
        using var legacy = new Android.Util.DisplayMetrics(); window.DefaultDisplay.GetRealMetrics(legacy);
        return (legacy.WidthPixels, legacy.HeightPixels, rotation);
    }
    public static async Task<bool> ExecutarAsync(EntradaCelular p, CancellationToken ct)
    {
        p.Validar(); ct.ThrowIfCancellationRequested(); var service = _atual;
        if (service is null || !EspelhoCelularService.Ativo || !EspelhoCelularService.SessaoValida(p.Sessao) || ((KeyguardManager)service.GetSystemService(KeyguardService)!).IsKeyguardLocked) return false;
        var tela = Tela();
        if (p.Rotacao != tela.Rotacao || Math.Abs(p.Largura / (double)p.Altura - tela.Largura / (double)tela.Altura) > .02) return false;
        if (p.Acao is "inicio" or "voltar" or "recentes") return service.PerformGlobalAction(p.Acao == "inicio" ? GlobalAction.Home : p.Acao == "voltar" ? GlobalAction.Back : GlobalAction.Recents);
        if (p.Acao is "texto" or "tecla") return service.Digitar(p);
        if (!OperatingSystem.IsAndroidVersionAtLeast(24)) return false;
        using var path = new Android.Graphics.Path(); path.MoveTo((float)(p.X * (tela.Largura - 1)), (float)(p.Y * (tela.Altura - 1)));
        if (p.Acao is "arrastar" or "rolar") path.LineTo((float)(p.DestinoX * (tela.Largura - 1)), (float)(p.DestinoY * (tela.Altura - 1)));
        var duration = p.Acao == "segurar" ? 650 : p.Acao == "tocar" ? 65 : p.DuracaoMs;
        using var stroke = new GestureDescription.StrokeDescription(path, 0, duration); using var builder = new GestureDescription.Builder(); builder.AddStroke(stroke);
        using var gesture = builder.Build(); var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callback = new Resultado(pending);
        if (!service.DispatchGesture(gesture!, callback, null)) return false;
        try { return await pending.Task.WaitAsync(TimeSpan.FromMilliseconds(duration + 2000), ct); } catch (TimeoutException) { return false; }
    }
    private bool Digitar(EntradaCelular p)
    {
        if (p.Acao == "tecla" && p.Tecla == "Escape") return PerformGlobalAction(GlobalAction.Back);
        using var root = RootInActiveWindow; using var node = root?.FindFocus(NodeFocus.Input);
        if (node is null || !node.Editable || !node.Enabled || node.Password) return false;
        if (p.Acao == "tecla" && p.Tecla == "Enter" && OperatingSystem.IsAndroidVersionAtLeast(30) && AccessibilityNodeInfo.AccessibilityAction.ActionImeEnter is { } ime && node.ActionList?.Any(a => a.Id == ime.Id) == true)
            return node.PerformAction((Android.Views.Accessibility.Action)ime.Id);
        var text = node.Text?.ToString() ?? ""; if (text.Length > 10000) return false;
        var start = Math.Clamp(node.TextSelectionStart, 0, text.Length); var end = Math.Clamp(node.TextSelectionEnd, start, text.Length);
        if (p.Acao == "tecla" && p.Tecla is "Left" or "Right" or "Up" or "Down")
        {
            var position = p.Tecla switch { "Left" => Math.Max(0, start - 1), "Right" => Math.Min(text.Length, end + 1), "Up" => 0, _ => text.Length };
            using var args = new Bundle(); args.PutInt(AccessibilityNodeInfo.ActionArgumentSelectionStartInt, position); args.PutInt(AccessibilityNodeInfo.ActionArgumentSelectionEndInt, position);
            return node.PerformAction(Android.Views.Accessibility.Action.SetSelection, args);
        }
        if (p.Acao == "tecla" && p.Tecla == "Backspace" && start == end && start > 0) { start--; if (start > 0 && char.IsLowSurrogate(text[start]) && char.IsHighSurrogate(text[start - 1])) start--; }
        var addition = p.Acao == "texto" ? p.Texto! : p.Tecla == "Enter" ? "\n" : p.Tecla == "Tab" ? "\t" : "";
        var value = text[..start] + addition + text[end..];
        using var bundle = new Bundle(); bundle.PutCharSequence(AccessibilityNodeInfo.ActionArgumentSetTextCharsequence, value);
        if (!node.PerformAction(Android.Views.Accessibility.Action.SetText, bundle)) return false;
        using var selection = new Bundle(); selection.PutInt(AccessibilityNodeInfo.ActionArgumentSelectionStartInt, start + addition.Length); selection.PutInt(AccessibilityNodeInfo.ActionArgumentSelectionEndInt, start + addition.Length);
        node.PerformAction(Android.Views.Accessibility.Action.SetSelection, selection); return true;
    }
    private sealed class Resultado(TaskCompletionSource<bool> result) : GestureResultCallback
    {
        public override void OnCompleted(GestureDescription? gestureDescription) => result.TrySetResult(true);
        public override void OnCancelled(GestureDescription? gestureDescription) => result.TrySetResult(false);
    }
}
