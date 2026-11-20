using Jarvis.Core;
using Jarvis.Desktop.Renderers;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Jarvis.Mobile.Views;

public sealed class ReatorView : SKCanvasView
{
    private readonly System.Diagnostics.Stopwatch _relogio = System.Diagnostics.Stopwatch.StartNew();
    private readonly IDispatcherTimer _timer;
    private bool _visivel;
    private bool _ouvindo;
    private double _nivel, _alvo, _ultimo;

    public ReatorView()
    {
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(1000d / 30);
        _timer.Tick += (_, _) =>
        {
            var agora = _relogio.Elapsed.TotalSeconds;
            _nivel = FormaEsferaAudio.Suavizar(_nivel, _ouvindo ? _alvo : 0, agora - _ultimo);
            _ultimo = agora;
            if (!_ouvindo && _nivel < .004)
            {
                _nivel = 0;
                _timer.Stop();
            }
            InvalidateSurface();
        };
        Loaded += (_, _) => App.AtividadeMudou += AoMudarAtividade;
        Unloaded += (_, _) =>
        {
            App.AtividadeMudou -= AoMudarAtividade;
            Animar(false);
        };
    }

    private void AoMudarAtividade(bool ativo)
    {
        if (ativo) Ouvir(_ouvindo);
        else _timer.Stop();
    }

    public double Nivel { get => _alvo; set => _alvo = value; }

    public void Ouvir(bool ativo)
    {
        _ouvindo = ativo;
        if (!ativo) _alvo = 0;
        if (_visivel && App.EmPrimeiroPlano && !_timer.IsRunning && (ativo || _nivel > 0))
        {
            _ultimo = _relogio.Elapsed.TotalSeconds;
            _timer.Start();
        }
    }

    public void Animar(bool visivel)
    {
        _visivel = visivel;
        if (!visivel)
        {
            _timer.Stop();
            _nivel = 0;
        }
        else Ouvir(_ouvindo);
        InvalidateSurface();
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e) =>
        AudioOrbRenderer.Desenhar(e.Surface.Canvas, e.Info.Width, e.Info.Height, _relogio.Elapsed.TotalSeconds, _nivel,
            _ouvindo || _nivel > 0 ? ReactorState.Ouvindo : ReactorState.Repouso);
}
