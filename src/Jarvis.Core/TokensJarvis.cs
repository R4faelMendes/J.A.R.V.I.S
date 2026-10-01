// <summary>
// Tokens do sistema de design JARVIS em C#.
// Espelha src/design/tokens.css para o WPF/MAUI usar sem duplicar valores.
// </summary>
namespace Jarvis.Core.Design;

public static class TokensJarvis
{
    public const string Abismo = "#050D18";
    public const string Vidro = "#0C1B2E";
    public const string Linha = "#1B3A57";
    public const string Ciano = "#4FD6FF";
    public const string Gelo = "#E6F6FF";
    public const string Nevoa = "#8FA9BF";
    public const string Ouro = "#F4B544";
    public const string Vermelho = "#FF4D3D";
    public const string Verde = "#4DE3A1";

    public const double RaioBotao = 10;
    public const double RaioPainel = 20;
    public const double RaioJanela = 28;

    public const int Grade = 8;

    public static readonly int[] EscalaTexto = [12, 14, 16, 20, 28, 40, 64];
}
