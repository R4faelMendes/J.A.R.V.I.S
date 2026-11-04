namespace Jarvis.Mobile.Views;

/// <summary>Reator com anéis e globo projetados em 3D, animados individualmente.</summary>
public sealed class ReactorArt : ContentView
{
    private readonly ReatorView _reactor = new();
    public double Nivel { get => _reactor.Nivel; set => _reactor.Nivel = value; }
    public ReactorArt() { Content = _reactor; var tap = new TapGestureRecognizer(); tap.Tapped += (_, _) => _reactor.AlternarMontagem(); GestureRecognizers.Add(tap); }
    public void Animar(bool active) => _reactor.Animar(active);
    public static void DefinirAppAtivo(bool active) => ReatorView.DefinirAppAtivo(active);
}
