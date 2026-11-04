using Android.Views;
using Jarvis.Core;
namespace Jarvis.Mobile.Services;

internal sealed class ToquesTelaAndroid : Java.Lang.Object, IDisposable
{
    private readonly Android.Views.View _view;
    private readonly Func<bool> _ativo;
    private readonly Func<float, float, PontoTela?> _map;
    private readonly Action<string, PontoTela, PontoTela, int, int> _send;
    private readonly GestureDetector _detector;
    private float _x, _y, _scrollStart, _scrollEnd;
    private long _down;
    private bool _arrastou, _multi, _longo;
    public ToquesTelaAndroid(Android.Views.View view, Func<bool> ativo, Func<float, float, PontoTela?> map, Action<string, PontoTela, PontoTela, int, int> send)
    {
        _view = view; _ativo = ativo; _map = map; _send = send;
        _detector = new GestureDetector(view.Context!, new Listener(this));
        _view.Touch += Touch;
    }
    private void Emitir(string acao, float x, float y)
    {
        if (_ativo() && _map(x, y) is { } p) _send(acao, p, p, 0, 350);
    }
    private void Touch(object? sender, Android.Views.View.TouchEventArgs args)
    {
        if (!_ativo() || args.Event is not { } e) { args.Handled = false; return; }
        args.Handled = true;
        if (e.ActionMasked == MotionEventActions.Down) { _x = e.GetX(); _y = e.GetY(); _down = e.EventTime; _arrastou = _multi = _longo = false; }
        if (e.ActionMasked == MotionEventActions.PointerDown && e.PointerCount >= 2)
        {
            _multi = true; _scrollStart = _scrollEnd = e.GetY();
            using var cancel = MotionEvent.Obtain(e); if (cancel is not null) { cancel.Action = MotionEventActions.Cancel; _detector.OnTouchEvent(cancel); }
        }
        if (_multi && e.ActionMasked == MotionEventActions.Move) _scrollEnd = e.GetY();
        if (!_multi) _detector.OnTouchEvent(e);
        if (e.ActionMasked != MotionEventActions.Up) return;
        if (_multi)
        {
            var ticks = Math.Clamp((int)Math.Round((_scrollEnd - _scrollStart) / (32 * (_view.Resources?.DisplayMetrics?.Density ?? 1))), -12, 12);
            if (ticks != 0 && _map(_x, _y) is { } p) _send("rolar", p, p, ticks, 350);
        }
        else if (_arrastou && !_longo && _map(_x, _y) is { } start && _map(e.GetX(), e.GetY()) is { } end)
            _send("arrastar", start, end, 0, (int)Math.Clamp(e.EventTime - _down, 100, 1000));
    }
    private sealed class Listener(ToquesTelaAndroid owner) : GestureDetector.SimpleOnGestureListener
    {
        public override bool OnDown(MotionEvent e) => true;
        public override bool OnSingleTapConfirmed(MotionEvent e) { if (!owner._multi) owner.Emitir("clique", e.GetX(), e.GetY()); return true; }
        public override bool OnDoubleTap(MotionEvent e) { if (!owner._multi) owner.Emitir("duplo", e.GetX(), e.GetY()); return true; }
        public override void OnLongPress(MotionEvent e) { if (!owner._multi) { owner._longo = true; owner.Emitir("direito", e.GetX(), e.GetY()); } }
        public override bool OnScroll(MotionEvent? e1, MotionEvent e2, float distanceX, float distanceY) { owner._arrastou = true; return true; }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { _view.Touch -= Touch; _detector.Dispose(); }
        base.Dispose(disposing);
    }
}
