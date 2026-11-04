namespace Jarvis.Core;

/// <summary>Anéis rígidos giram sobre os próprios eixos; sem rotação artificial do conjunto no plano da tela.</summary>
public static class ReatorCinematica
{
    public static double Fase(int anel, double segundos) => segundos * (anel == 0 ? .2094395102 : anel == 1 ? -.1570796327 : .1221730476);
    public static (double X, double Y, double Z) Posicao(int anel, double segundos, double angulo, double raio, double altura)
    {
        angulo += Fase(anel, segundos);
        var x = Math.Cos(angulo) * raio; var z = Math.Sin(angulo) * raio;
        var inclinacao = .83 + anel * .63;
        var y = altura * Math.Cos(inclinacao) - z * Math.Sin(inclinacao);
        z = altura * Math.Sin(inclinacao) + z * Math.Cos(inclinacao);
        var eixo = .30 + anel * .82;
        return (x * Math.Cos(eixo) - y * Math.Sin(eixo), x * Math.Sin(eixo) + y * Math.Cos(eixo), z);
    }
}
