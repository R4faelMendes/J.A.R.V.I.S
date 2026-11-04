namespace Jarvis.Mobile.Views;

/// <summary>Apresenta quadros diretamente no controle Android, sem recarregar ImageSource a cada frame.</summary>
internal sealed class QuadroAoVivo : ContentView
{
    private readonly Image _image = new() { Aspect = Aspect.AspectFit };
    private int _quadroLargura, _quadroAltura;
    public bool ControleAtivo { get; set; }
    public string? MonitorId { get; set; }
    public event Action<Jarvis.Core.PedidoToque>? Gesto;
#if ANDROID
    private Android.Graphics.Bitmap? _bitmap;
    private Services.ToquesTelaAndroid? _toques;
#endif
    public QuadroAoVivo()
    {
        Content = _image;
#if ANDROID
        void Conectar()
        {
            _toques?.Dispose(); _toques = null;
            if (_image.Handler?.PlatformView is Android.Widget.ImageView view)
                _toques = new(view, () => ControleAtivo && _quadroLargura > 0, (x, y) => Jarvis.Core.CoordenadasTela.Normalizar(x, y, view.Width, view.Height, _quadroLargura, _quadroAltura),
                    (acao, start, end, ticks, ms) => { if (MonitorId is { } monitor && ControleAtivo) Gesto?.Invoke(new(monitor, acao, start.X, start.Y, end.X, end.Y, ticks, ms)); });
        }
        _image.HandlerChanged += (_, _) => Conectar();
        Loaded += (_, _) => Conectar(); Unloaded += (_, _) => { _toques?.Dispose(); _toques = null; };
#else
        var tap = new TapGestureRecognizer(); tap.Tapped += (_, e) =>
        {
            if (!ControleAtivo || MonitorId is not { } monitor || e.GetPosition(_image) is not { } p) return;
            if (Jarvis.Core.CoordenadasTela.Normalizar(p.X, p.Y, _image.Width, _image.Height, _quadroLargura, _quadroAltura) is { } point) Gesto?.Invoke(new(monitor, "clique", point.X, point.Y));
        }; _image.GestureRecognizers.Add(tap);
#endif
    }
    public async Task ApresentarAsync(byte[] jpeg, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
#if ANDROID
        var bitmap = await Task.Run(() => Android.Graphics.BitmapFactory.DecodeByteArray(jpeg, 0, jpeg.Length), ct);
        if (bitmap is null) throw new InvalidOperationException("Não foi possível decodificar o quadro.");
        try
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ct.ThrowIfCancellationRequested();
                if (_image.Handler?.PlatformView is not Android.Widget.ImageView view) throw new InvalidOperationException("Visualizador ainda não está pronto.");
                _quadroLargura = bitmap.Width; _quadroAltura = bitmap.Height;
                view.SetImageBitmap(bitmap); var old = _bitmap; _bitmap = bitmap; bitmap = null; old?.Dispose();
            });
        }
        finally { bitmap?.Dispose(); }
#else
        using (var decoded = SkiaSharp.SKBitmap.Decode(jpeg)) { _quadroLargura = decoded.Width; _quadroAltura = decoded.Height; }
        await MainThread.InvokeOnMainThreadAsync(() => { ct.ThrowIfCancellationRequested(); _image.Source = ImageSource.FromStream(() => new MemoryStream(jpeg, false)); });
#endif
    }
    public void Limpar()
    {
        _quadroLargura = _quadroAltura = 0;
#if ANDROID
        if (_image.Handler?.PlatformView is Android.Widget.ImageView view) view.SetImageDrawable(null);
        _bitmap?.Dispose(); _bitmap = null;
#endif
        _image.Source = null;
    }
}
