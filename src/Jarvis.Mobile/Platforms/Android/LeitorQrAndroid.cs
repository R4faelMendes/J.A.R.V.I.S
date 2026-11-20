using Android.App;
using Android.Views;
using ZXing;
using Camera = Android.Hardware.Camera;

namespace Jarvis.Mobile.Services;

internal sealed class LeitorQrAndroid : Java.Lang.Object, ISurfaceHolderCallback, Camera.IPreviewCallback
{
    private readonly TaskCompletionSource<string?> _resultado = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly BarcodeReaderGeneric _leitor = new() { AutoRotate = true };
    private Camera? _camera;
    private int _largura, _altura;
    private long _ultimo;
    private int _lendo;

    public static async Task<string?> LerAsync()
    {
        var activity = Platform.CurrentActivity ?? throw new InvalidOperationException("Câmera indisponível.");
        using var scanner = new LeitorQrAndroid();
        scanner._leitor.Options.PossibleFormats = [BarcodeFormat.QR_CODE];
        using var superficie = new SurfaceView(activity);
        superficie.SetMinimumHeight((int)(240 * activity.Resources!.DisplayMetrics!.Density));
        var holder = superficie.Holder!;
        holder.AddCallback(scanner);
        using var janela = new AlertDialog.Builder(activity)
            .SetTitle("Aponte para o QR de pareamento")
            .SetView(superficie)
            .SetNegativeButton("Cancelar", (_, _) => scanner._resultado.TrySetResult(null))
            .Create();
        janela.DismissEvent += (_, _) => scanner._resultado.TrySetResult(null);
        janela.Show();
        try { return await scanner._resultado.Task; }
        finally { janela.Dismiss(); holder.RemoveCallback(scanner); scanner.Fechar(); }
    }

    public void SurfaceCreated(ISurfaceHolder holder)
    {
        try
        {
            _camera = Camera.Open() ?? throw new InvalidOperationException("Câmera traseira indisponível.");
            using var parametros = _camera.GetParameters()!;
            using var size = parametros.PreviewSize!;
            _largura = size.Width;
            _altura = size.Height;
            if (parametros.SupportedFocusModes?.Contains(Camera.Parameters.FocusModeContinuousPicture) == true)
            {
                parametros.FocusMode = Camera.Parameters.FocusModeContinuousPicture;
                _camera.SetParameters(parametros);
            }
            _camera.SetDisplayOrientation(90);
            _camera.SetPreviewDisplay(holder);
            _camera.SetPreviewCallback(this);
            _camera.StartPreview();
        }
        catch (Exception ex) { _resultado.TrySetException(ex); }
    }

    public void SurfaceChanged(ISurfaceHolder holder, Android.Graphics.Format format, int width, int height) { }
    public void SurfaceDestroyed(ISurfaceHolder holder) { Fechar(); _resultado.TrySetResult(null); }

    public void OnPreviewFrame(byte[]? data, Camera? camera)
    {
        if (data is null || _resultado.Task.IsCompleted || Environment.TickCount64 - _ultimo < 300 ||
            Interlocked.Exchange(ref _lendo, 1) != 0) return;
        _ultimo = Environment.TickCount64;
        var largura = _largura;
        var altura = _altura;
        var pixels = data.AsSpan(0, Math.Min(data.Length, largura * altura)).ToArray();
        _ = Task.Run(() =>
        {
            try
            {
                if (pixels.Length != largura * altura) return;
                var qr = _leitor.Decode(new PlanarYUVLuminanceSource(pixels, largura, altura, 0, 0, largura, altura, false))?.Text;
                if (qr?.StartsWith("jarvis://parear?", StringComparison.OrdinalIgnoreCase) == true)
                    _resultado.TrySetResult(qr);
                else if (Uri.TryCreate(qr, UriKind.Absolute, out var url) && url.AbsolutePath == "/mobile")
                {
                    var link = Uri.UnescapeDataString(url.Fragment.TrimStart('#'));
                    if (link.StartsWith("jarvis://parear?", StringComparison.OrdinalIgnoreCase)) _resultado.TrySetResult(link);
                }
            }
            catch (Exception ex) { _resultado.TrySetException(ex); }
            finally { Volatile.Write(ref _lendo, 0); }
        });
    }

    private void Fechar()
    {
        if (_camera is not { } camera) return;
        _camera = null;
        try { camera.SetPreviewCallback(null); camera.StopPreview(); }
        finally { camera.Release(); camera.Dispose(); }
    }
}
