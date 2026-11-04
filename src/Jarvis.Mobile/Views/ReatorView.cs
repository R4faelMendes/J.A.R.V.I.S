// JARVIS.Mobile — reator portátil (SkiaSharp, mesmo visual do desktop, leve).
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Jarvis.Mobile.Views;

public sealed class ReatorView : SKCanvasView
{
    public static readonly BindableProperty NivelProperty =
        BindableProperty.Create(nameof(Nivel), typeof(double), typeof(ReatorView), 0.05);
    public double Nivel { get => (double)GetValue(NivelProperty); set => SetValue(NivelProperty, value); }

    private readonly System.Diagnostics.Stopwatch _rel = System.Diagnostics.Stopwatch.StartNew();
    private readonly Microsoft.Maui.Dispatching.IDispatcherTimer _timer;
    private static event Action<bool>? EstadoApp;
    private bool _ativa;
    private double _montagem;
    private double _destino = 1;
    public void AlternarMontagem() { _destino = _destino == 1 ? 0 : 1; InvalidateSurface(); }
    private Page? _pagina;
    private void PaginaApareceu(object? s, EventArgs e) => Animar(true);
    private void PaginaSaiu(object? s, EventArgs e) => Animar(false);
    public static void DefinirAppAtivo(bool ativo) => EstadoApp?.Invoke(ativo);
    public void Animar(bool ativo) { _ativa = ativo; if (ativo) _timer.Start(); else _timer.Stop(); }
    private void AoMudarApp(bool ativo) { if (ativo && _ativa) _timer.Start(); else _timer.Stop(); }

    public ReatorView()
    {
        _timer = Dispatcher.CreateTimer(); _timer.Interval = TimeSpan.FromMilliseconds(1000.0 / 30);
        _timer.Tick += (_, _) => { _timer.Interval = TimeSpan.FromMilliseconds(Services.EspelhoCelular.Ativo ? 125 : 40); var passo = _timer.Interval.TotalSeconds / 3.0; _montagem = _destino > _montagem ? Math.Min(_destino, _montagem + passo) : Math.Max(_destino, _montagem - passo); InvalidateSurface(); };
        Loaded += (_, _) => { EstadoApp += AoMudarApp; for (Element? p = Parent; p is not null; p = p.Parent) if (p is Page page) { _pagina = page; page.Appearing += PaginaApareceu; page.Disappearing += PaginaSaiu; break; } Animar(true); };
        Unloaded += (_, _) => { EstadoApp -= AoMudarApp; if (_pagina is not null) { _pagina.Appearing -= PaginaApareceu; _pagina.Disappearing -= PaginaSaiu; _pagina = null; } Animar(false); };
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        Jarvis.Rendering.ReatorRealista.Desenhar(e.Surface.Canvas, e.Info.Width, e.Info.Height, _rel.Elapsed.TotalSeconds, Nivel, _montagem);
    }
}

