using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
namespace Jarvis.Mobile;
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(new[] { Intent.ActionView }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataScheme = "jarvis", DataHost = "parear")]
public class MainActivity : MauiAppCompatActivity
{
    private TaskCompletionSource<string?>? _speech;
    private TaskCompletionSource? _projecao;
    public Task AutorizarEspelhoAsync()
    {
        if (_projecao is not null) throw new InvalidOperationException("A autorização de captura já está aberta.");
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); _projecao = pending;
        try { var manager = (Android.Media.Projection.MediaProjectionManager)GetSystemService(MediaProjectionService)!; using var request = CapturaCompleta(manager); StartActivityForResult(request, 7645); }
        catch (Exception ex) { _projecao = null; pending.TrySetException(ex); }
        return pending.Task;
    }
    private static Intent CapturaCompleta(Android.Media.Projection.MediaProjectionManager manager)
    {
        // API 34/QPR: a referência .NET 8 não contém este overload; pedir o display inteiro quando disponível.
        if (OperatingSystem.IsAndroidVersionAtLeast(34))
        {
            try
            {
                using var type = Java.Lang.Class.ForName("android.media.projection.MediaProjectionConfig");
                using var factory = type!.GetMethod("createConfigForDefaultDisplay", []); using var config = factory!.Invoke(null, []);
                if (config is null) return manager.CreateScreenCaptureIntent();
                using var method = manager.Class!.GetMethod("createScreenCaptureIntent", [type]); using var result = method!.Invoke(manager, [config]);
                if (result is not null) { using var intent = result.JavaCast<Intent>(); return new Intent(intent); }
            }
            catch (Java.Lang.Exception) { }
        }
        return manager.CreateScreenCaptureIntent();
    }
    public Task<string?> OuvirAsync()
    {
        if (_speech is not null) throw new InvalidOperationException("O microfone já está em uso.");
        var pending = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously); _speech = pending;
        var intent = new Intent(Android.Speech.RecognizerIntent.ActionRecognizeSpeech);
        intent.PutExtra(Android.Speech.RecognizerIntent.ExtraLanguageModel, Android.Speech.RecognizerIntent.LanguageModelFreeForm);
        intent.PutExtra(Android.Speech.RecognizerIntent.ExtraLanguage, "pt-BR");
        if (OperatingSystem.IsAndroidVersionAtLeast(23)) intent.PutExtra(Android.Speech.RecognizerIntent.ExtraPreferOffline, true);
        intent.PutExtra(Android.Speech.RecognizerIntent.ExtraPrompt, "Diga um comando para o seu PC");
        try { StartActivityForResult(intent, 6421); }
        catch { _speech = null; throw new InvalidOperationException("Reconhecimento de voz indisponível. Confira o microfone ou use os controles do painel."); }
        return pending.Task;
    }
    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        if (requestCode == 7645)
        {
            var projectionPending = _projecao; _projecao = null;
            if (resultCode != Result.Ok || data is null) { projectionPending?.TrySetException(new InvalidOperationException("Compartilhamento não autorizado. Nenhuma tela foi enviada.")); return; }
            try { Services.EspelhoCelularService.Iniciar(this, (int)resultCode, data); projectionPending?.TrySetResult(); } catch (Exception ex) { projectionPending?.TrySetException(ex); }
            return;
        }
        if (requestCode != 6421) return;
        var pending = _speech; _speech = null;
        pending?.TrySetResult(resultCode == Result.Ok ? data?.GetStringArrayListExtra(Android.Speech.RecognizerIntent.ExtraResults)?.FirstOrDefault() : null);
    }
    protected override void OnDestroy() { _speech?.TrySetCanceled(); _speech = null; _projecao?.TrySetCanceled(); _projecao = null; base.OnDestroy(); }
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Encaminhar(Intent);
    }
    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Encaminhar(intent);
    }
    private static void Encaminhar(Intent? intent)
    {
        if (intent?.GetStringExtra("pagina") is { } pagina)
        {
            var route = pagina == "agenda" ? "//agenda" : pagina == "falar" ? "falar" : pagina == "aparelho" ? "aparelho" : "//controles"; App.PaginaPendente = route;
            MainThread.BeginInvokeOnMainThread(async () => { try { if (Shell.Current?.Handler is not null) { await Shell.Current.GoToAsync(route); App.PaginaPendente = null; } } catch { } });
        }
        if (intent?.DataString is { } link && link.StartsWith("jarvis://parear?", StringComparison.Ordinal))
            App.LinkPareamento = link;
    }
    protected override void OnResume() { base.OnResume(); Services.AvisosAndroid.Restaurar(this); }
}
