namespace Jarvis.Core;

public static class FormaEsferaAudio
{
    public static double Raio(double angulo, double tempo, double nivel)
    {
        var energia = double.IsFinite(nivel) ? Math.Clamp(nivel, 0, 1) : 0;
        // Contorno fechado e contínuo: a energia da fala cria cristas em toda a borda.
        var onda = .48 * Math.Sin(angulo * 5 + tempo * 3.6) + .28 * Math.Sin(angulo * 11 - tempo * 5.1) + .16 * Math.Sin(angulo * 23 + tempo * 7.2) + .08 * Math.Sin(angulo * 37 - tempo * 4.3);
        return 1 + energia * (.12 + onda * .17);
    }
    public static double Suavizar(double atual, double alvo, double segundos)
    {
        alvo = double.IsFinite(alvo) ? Math.Clamp(alvo, 0, 1) : 0;
        atual = double.IsFinite(atual) ? Math.Clamp(atual, 0, 1) : 0;
        var tau = alvo > atual ? .045 : .18;
        return atual + (alvo - atual) * (1 - Math.Exp(-Math.Clamp(segundos, 0, .25) / tau));
    }
}
