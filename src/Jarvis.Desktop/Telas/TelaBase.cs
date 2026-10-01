using System.Windows;
using System.Windows.Controls;
using Jarvis.Desktop.Controles;

namespace Jarvis.Desktop.Telas;

/// <summary>
/// Tela de espaço reservado da fase 1 (base visual).
/// Cada tela real entra aqui nas próximas fases, mantendo o shell.
/// </summary>
public sealed class TelaBase : StackPanel
{
    public TelaBase(string titulo)
    {
        Margin = new Thickness(8);
        var cabecalho = new TextBlock
        {
            Text = titulo,
            FontFamily = (System.Windows.Media.FontFamily)Application.Current.Resources["FonteTitulo"],
            FontSize = 40,
        };
        Children.Add(cabecalho);
        Children.Add(new TextBlock
        {
            Text = "Esta tela chega na próxima fase. O shell, o tema e o reator já estão prontos.",
            Foreground = (System.Windows.Media.Brush)Application.Current.Resources["Texto2"],
            FontSize = 14,
        });
        if (titulo == "Núcleo")
        {
            Children.Add(new VisaoReator { Width = 420, Height = 420 });
        }
    }
}
