namespace Jarvis.Mobile.Views;

public sealed class ReactorArt : ContentView
{
    private readonly ReatorView _reactor = new();

    public ReactorArt() => Content = _reactor;
    public double Nivel { get => _reactor.Nivel; set => _reactor.Nivel = value; }
    public void Animar(bool ativo) => _reactor.Animar(ativo);
    public void Ouvir(bool ativo) => _reactor.Ouvir(ativo);
}
