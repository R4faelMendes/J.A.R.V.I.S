namespace Jarvis.Core;

public readonly record struct PontoFluxo(double X, double Y);
public sealed record TrilhaFluxo(PontoFluxo[] Pontos, double Opacidade, double Espessura, bool Ambar);
/// <summary>Órbitas de energia e filamentos animados, iguais nos renderizadores WPF/Skia.</summary>
public static class ReactorFluxo
{
    public static IReadOnlyList<TrilhaFluxo> Gerar(double segundos, double audio)
    {
        var trilhas = new List<TrilhaFluxo>(10); audio = Math.Clamp(audio, 0, 1);
        for (var anel = 0; anel < 3; anel++)
        {
            var giro = new[] { -.65, .7, 1.35 }[anel] + .14 * Math.Sin(segundos * .45 + anel * 2);
            for (var cauda = 0; cauda < 2; cauda++)
            {
                var pontos = new PontoFluxo[40];
                var fase = segundos * (anel % 2 == 0 ? .7 : -.55) + anel * 2.1 + cauda * Math.PI;
                for (var i = 0; i < pontos.Length; i++)
                {
                    var ang = fase + i * .032;
                    var x = Math.Cos(ang) * (.275 - anel * .025); var y = Math.Sin(ang) * (.115 + anel * .02);
                    pontos[i] = new(.515 + x * Math.Cos(giro) - y * Math.Sin(giro), .435 + x * Math.Sin(giro) + y * Math.Cos(giro));
                }
                trilhas.Add(new(pontos, .55 + audio * .3, .0018 + audio * .001, anel == 1 && cauda == 1));
            }
        }
        for (var filamento = 0; filamento < 4; filamento++)
        {
            var pontos = new PontoFluxo[48];
            for (var i = 0; i < pontos.Length; i++)
            {
                var ang = i / 47d * Math.PI * 2;
                var r = .058 + .015 * Math.Sin(ang * 3 + segundos * 2 + filamento) + audio * .015;
                var giro = segundos * .6 + filamento * Math.PI / 4;
                var x = Math.Cos(ang) * r; var y = Math.Sin(ang) * r * .65;
                pontos[i] = new(.515 + x * Math.Cos(giro) - y * Math.Sin(giro), .435 + x * Math.Sin(giro) + y * Math.Cos(giro));
            }
            trilhas.Add(new(pontos, .35 + audio * .4, .0011, false));
        }
        return trilhas;
    }
}
