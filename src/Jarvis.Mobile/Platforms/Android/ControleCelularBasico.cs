using Android.App;
using Android.Graphics;
using Android.Views;
using Jarvis.Core;
namespace Jarvis.Mobile.Services;

/// <summary>Instalador padrão sem serviço de acessibilidade e sem leitura de outros aplicativos.</summary>
public static class ControleCelularAndroid
{
    public static bool Suportado => false;
    public static bool Ativo => false;
    public static void Configurar() => throw new NotSupportedException("O instalador padrão não solicita Acessibilidade do Android.");
    public static Task<bool> ExecutarAsync(EntradaCelular p, CancellationToken ct) => Task.FromResult(false);
    public static Task<Bitmap?> CapturarCompativelAsync(Guid sessao, CancellationToken ct) => Task.FromResult<Bitmap?>(null);
    public static (int Largura, int Altura, int Rotacao) Tela()
    {
        var window = (IWindowManager)Android.App.Application.Context.GetSystemService(Service.WindowService)!;
        var rotation = (int)window.DefaultDisplay!.Rotation;
        if (OperatingSystem.IsAndroidVersionAtLeast(30)) { using var metrics = window.MaximumWindowMetrics; using var bounds = metrics!.Bounds; return (bounds.Width(), bounds.Height(), rotation); }
        using var legacy = new Android.Util.DisplayMetrics(); window.DefaultDisplay.GetRealMetrics(legacy); return (legacy.WidthPixels, legacy.HeightPixels, rotation);
    }
}
